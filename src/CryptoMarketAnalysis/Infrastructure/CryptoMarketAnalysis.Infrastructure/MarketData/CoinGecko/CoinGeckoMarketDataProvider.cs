using CryptoMarketAnalysis.Application.Abstractions.MarketData;
using CryptoMarketAnalysis.Application.Contracts.MarketData.External;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CryptoMarketAnalysis.Infrastructure.MarketData.CoinGecko;

public sealed class CoinGeckoMarketDataProvider : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly CoinGeckoOptions _options;

    public CoinGeckoMarketDataProvider(
        HttpClient httpClient,
        IOptions<CoinGeckoOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            throw new InvalidOperationException("CoinGecko BaseUrl cannot be empty.");

        _httpClient.BaseAddress = new Uri(
            _options.BaseUrl.TrimEnd('/') + "/",
            UriKind.Absolute);

        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(_options.DemoApiKey) &&
            !_httpClient.DefaultRequestHeaders.Contains("x-cg-demo-api-key"))
        {
            _httpClient.DefaultRequestHeaders.Add(
                "x-cg-demo-api-key",
                _options.DemoApiKey);
        }
    }

    public string SourceCode => "COINGECKO";

    public async Task<IReadOnlyCollection<ExternalMarketDataPointDto>> GetHistoricalAsync(
        string symbol,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        if (fromUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("From date must be in UTC.", nameof(fromUtc));

        if (toUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("To date must be in UTC.", nameof(toUtc));

        if (fromUtc >= toUtc)
            throw new ArgumentException("From date must be earlier than to date.", nameof(fromUtc));

        string coinId = CoinGeckoSymbolMapper.ToCoinId(symbol);

        long from = new DateTimeOffset(fromUtc).ToUnixTimeSeconds();
        long to = new DateTimeOffset(toUtc).ToUnixTimeSeconds();

        string requestUri =
            $"coins/{Uri.EscapeDataString(coinId)}/market_chart/range" +
            $"?vs_currency={Uri.EscapeDataString(_options.VsCurrency)}" +
            $"&from={from.ToString(CultureInfo.InvariantCulture)}" +
            $"&to={to.ToString(CultureInfo.InvariantCulture)}" +
            $"&interval={Uri.EscapeDataString(_options.Interval)}";

        using HttpResponseMessage response = await SendAsync(
            requestUri,
            cancellationToken);

        string json = await ReadResponseContentAsync(
            response,
            cancellationToken);

        return ParseHistoricalResponse(json);
    }

    public async Task<ExternalMarketDataPointDto?> GetLatestAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        string coinId = CoinGeckoSymbolMapper.ToCoinId(symbol);

        string requestUri =
            "simple/price" +
            $"?ids={Uri.EscapeDataString(coinId)}" +
            $"&vs_currencies={Uri.EscapeDataString(_options.VsCurrency)}" +
            "&include_market_cap=true" +
            "&include_24hr_vol=true" +
            "&include_last_updated_at=true";

        using HttpResponseMessage response = await SendAsync(
            requestUri,
            cancellationToken);

        string json = await ReadResponseContentAsync(
            response,
            cancellationToken);

        return ParseLatestResponse(
            json,
            coinId,
            _options.VsCurrency);
    }

    private static async Task<string> ReadResponseContentAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("CoinGecko returned empty response.");

        return json;
    }

    private static List<ExternalMarketDataPointDto> ParseHistoricalResponse(
        string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);

            JsonElement root = document.RootElement;

            Dictionary<long, decimal> prices = ReadTimeSeries(
                root,
                "prices");

            Dictionary<long, decimal> marketCaps = ReadTimeSeries(
                root,
                "market_caps");

            Dictionary<long, decimal> volumes = ReadTimeSeries(
                root,
                "total_volumes");

            return prices
                .OrderBy(pair => pair.Key)
                .Select(pair =>
                {
                    DateTime timestampUtc = DateTimeOffset
                        .FromUnixTimeMilliseconds(pair.Key)
                        .UtcDateTime;

                    marketCaps.TryGetValue(pair.Key, out decimal marketCapUsd);
                    volumes.TryGetValue(pair.Key, out decimal volume24hUsd);

                    return new ExternalMarketDataPointDto(
                        timestampUtc,
                        pair.Value,
                        marketCaps.ContainsKey(pair.Key) ? marketCapUsd : null,
                        volumes.ContainsKey(pair.Key) ? volume24hUsd : null);
                })
                .ToList();
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "CoinGecko returned invalid historical JSON response.",
                exception);
        }
    }

    private static ExternalMarketDataPointDto? ParseLatestResponse(
        string json,
        string coinId,
        string vsCurrency)
    {
        try
        {
            using var document = JsonDocument.Parse(json);

            JsonElement root = document.RootElement;

            if (!root.TryGetProperty(coinId, out JsonElement coinElement))
                return null;

            string currency = vsCurrency.ToLowerInvariant();
            string marketCapProperty = $"{currency}_market_cap";
            string volumeProperty = $"{currency}_24h_vol";

            if (!TryGetDecimal(coinElement, currency, out decimal priceUsd))
                return null;

            decimal? marketCapUsd = TryGetDecimal(
                coinElement,
                marketCapProperty,
                out decimal marketCap)
                    ? marketCap
                    : null;

            decimal? volume24hUsd = TryGetDecimal(
                coinElement,
                volumeProperty,
                out decimal volume)
                    ? volume
                    : null;

            if (!TryGetInt64(coinElement, "last_updated_at", out long lastUpdatedAt))
                throw new InvalidOperationException("CoinGecko latest response does not contain last_updated_at.");

            DateTime timestampUtc = DateTimeOffset
                .FromUnixTimeSeconds(lastUpdatedAt)
                .UtcDateTime;

            return new ExternalMarketDataPointDto(
                timestampUtc,
                priceUsd,
                marketCapUsd,
                volume24hUsd);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "CoinGecko returned invalid latest JSON response.",
                exception);
        }
    }

    private static Dictionary<long, decimal> ReadTimeSeries(
        JsonElement root,
        string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement seriesElement))
            return new Dictionary<long, decimal>();

        if (seriesElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"CoinGecko field '{propertyName}' has invalid format.");

        var result = new Dictionary<long, decimal>();

        foreach (JsonElement item in seriesElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Array || item.GetArrayLength() < 2)
                continue;

            JsonElement timestampElement = item[0];
            JsonElement valueElement = item[1];

            if (!TryGetInt64(timestampElement, out long timestampMilliseconds))
                continue;

            if (!TryGetDecimal(valueElement, out decimal value))
                continue;

            result[timestampMilliseconds] = value;
        }

        return result;
    }

    private static bool TryGetDecimal(
        JsonElement element,
        string propertyName,
        out decimal value)
    {
        value = default;

        if (!element.TryGetProperty(propertyName, out JsonElement property))
            return false;

        return TryGetDecimal(property, out value);
    }

    private static bool TryGetDecimal(
        JsonElement element,
        out decimal value)
    {
        value = default;

        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetDecimal(out value),
            JsonValueKind.String => decimal.TryParse(
                element.GetString(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out value),
            JsonValueKind.Undefined => throw new NotImplementedException(),
            JsonValueKind.Object => throw new NotImplementedException(),
            JsonValueKind.Array => throw new NotImplementedException(),
            JsonValueKind.True => throw new NotImplementedException(),
            JsonValueKind.False => throw new NotImplementedException(),
            JsonValueKind.Null => throw new NotImplementedException(),
            _ => false,
        };
    }

    private static bool TryGetInt64(
        JsonElement element,
        string propertyName,
        out long value)
    {
        value = default;

        if (!element.TryGetProperty(propertyName, out JsonElement property))
            return false;

        return TryGetInt64(property, out value);
    }

    private static bool TryGetInt64(
        JsonElement element,
        out long value)
    {
        value = default;

        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt64(out value),
            JsonValueKind.String => long.TryParse(
                element.GetString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value),
            JsonValueKind.Undefined => throw new NotImplementedException(),
            JsonValueKind.Object => throw new NotImplementedException(),
            JsonValueKind.Array => throw new NotImplementedException(),
            JsonValueKind.True => throw new NotImplementedException(),
            JsonValueKind.False => throw new NotImplementedException(),
            JsonValueKind.Null => throw new NotImplementedException(),
            _ => false,
        };
    }

    private async Task<HttpResponseMessage> SendAsync(
        string requestUri,
        CancellationToken cancellationToken)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(
                requestUri,
                cancellationToken);

            if (response.IsSuccessStatusCode)
                return response;

            string statusCode = ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture);

            throw new InvalidOperationException(
                $"CoinGecko request failed with status code {statusCode} {response.StatusCode}.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            throw new InvalidOperationException(
                "CoinGecko request failed due to network error.",
                exception);
        }
    }
}