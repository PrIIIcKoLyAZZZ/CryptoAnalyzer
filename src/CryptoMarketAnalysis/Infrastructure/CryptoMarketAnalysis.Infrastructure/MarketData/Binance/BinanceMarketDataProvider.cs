using CryptoMarketAnalysis.Application.Abstractions.MarketData;
using CryptoMarketAnalysis.Application.Contracts.MarketData.External;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace CryptoMarketAnalysis.Infrastructure.MarketData.Binance;

public sealed class BinanceMarketDataProvider : IMarketDataProvider
{
    private const int MaxBinanceLimit = 1000;

    private readonly HttpClient _httpClient;
    private readonly BinanceOptions _options;
    private readonly BinanceSymbolMapper _symbolMapper;

    public BinanceMarketDataProvider(
        HttpClient httpClient,
        IOptions<BinanceOptions> options,
        BinanceSymbolMapper symbolMapper)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _symbolMapper = symbolMapper;
    }

    public string SourceCode => "BINANCE";

    public async Task<IReadOnlyCollection<ExternalMarketDataPointDto>> GetHistoricalAsync(
        string symbol,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        if (fromUtc.Kind != DateTimeKind.Utc)
        {
            throw new InvalidOperationException("fromUtc must be UTC.");
        }

        if (toUtc.Kind != DateTimeKind.Utc)
        {
            throw new InvalidOperationException("toUtc must be UTC.");
        }

        if (fromUtc >= toUtc)
        {
            throw new InvalidOperationException("fromUtc must be earlier than toUtc.");
        }

        string binanceSymbol = _symbolMapper.MapToBinanceSymbol(symbol);
        int limit = Math.Clamp(_options.Limit, 1, MaxBinanceLimit);

        List<ExternalMarketDataPointDto> result = [];
        DateTime currentFromUtc = fromUtc;

        while (currentFromUtc < toUtc)
        {
            long startTime = ToUnixMilliseconds(currentFromUtc);
            long endTime = ToUnixMilliseconds(toUtc);

            string requestUri =
                $"/api/v3/klines?symbol={Uri.EscapeDataString(binanceSymbol)}" +
                $"&interval={Uri.EscapeDataString(_options.Interval)}" +
                $"&startTime={startTime}" +
                $"&endTime={endTime}" +
                $"&limit={limit}";

            using HttpResponseMessage response = await SendAsync(requestUri, cancellationToken);
            string content = await ReadRequiredContentAsync(response, cancellationToken);

            List<ExternalMarketDataPointDto> batch = ParseKlines(content);

            if (batch.Count == 0)
            {
                break;
            }

            result.AddRange(batch);

            DateTime lastTimestampUtc = batch.Max(point => point.TimestampUtc);
            DateTime nextFromUtc = lastTimestampUtc.AddMilliseconds(1);

            if (nextFromUtc <= currentFromUtc)
            {
                break;
            }

            currentFromUtc = nextFromUtc;

            if (batch.Count < limit)
            {
                break;
            }
        }

        return result;
    }

    public async Task<ExternalMarketDataPointDto?> GetLatestAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        string binanceSymbol = _symbolMapper.MapToBinanceSymbol(symbol);

        string requestUri =
            $"/api/v3/ticker/24hr?symbol={Uri.EscapeDataString(binanceSymbol)}";

        using HttpResponseMessage response = await SendAsync(requestUri, cancellationToken);
        string content = await ReadRequiredContentAsync(response, cancellationToken);

        return ParseLatest(content);
    }

    private static async Task<string> ReadRequiredContentAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        string content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Binance returned an empty response.");
        }

        return content;
    }

    private static List<ExternalMarketDataPointDto> ParseKlines(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Binance klines response has invalid format.");
            }

            List<ExternalMarketDataPointDto> points = [];

            foreach (JsonElement kline in document.RootElement.EnumerateArray())
            {
                if (kline.ValueKind != JsonValueKind.Array || kline.GetArrayLength() < 8)
                {
                    throw new InvalidOperationException("Binance kline item has invalid format.");
                }

                long openTime = kline[0].GetInt64();
                decimal closePrice = ParseDecimal(kline[4], "close price");
                decimal quoteVolume = ParseDecimal(kline[7], "quote asset volume");

                if (closePrice <= 0)
                {
                    throw new InvalidOperationException("Binance close price must be greater than zero.");
                }

                if (quoteVolume < 0)
                {
                    throw new InvalidOperationException("Binance quote volume must not be negative.");
                }

                points.Add(new ExternalMarketDataPointDto(
                    FromUnixMilliseconds(openTime),
                    closePrice,
                    null,
                    quoteVolume));
            }

            return points;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Binance klines response contains invalid JSON.", ex);
        }
    }

    private static ExternalMarketDataPointDto ParseLatest(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            JsonElement root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("Binance ticker response has invalid format.");
            }

            decimal lastPrice = ParseDecimal(root.GetProperty("lastPrice"), "lastPrice");
            decimal quoteVolume = ParseDecimal(root.GetProperty("quoteVolume"), "quoteVolume");
            long closeTime = root.GetProperty("closeTime").GetInt64();

            if (lastPrice <= 0)
            {
                throw new InvalidOperationException("Binance lastPrice must be greater than zero.");
            }

            if (quoteVolume < 0)
            {
                throw new InvalidOperationException("Binance quoteVolume must not be negative.");
            }

            return new ExternalMarketDataPointDto(
                FromUnixMilliseconds(closeTime),
                lastPrice,
                null,
                quoteVolume);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Binance ticker response contains invalid JSON.", ex);
        }
        catch (KeyNotFoundException ex)
        {
            throw new InvalidOperationException("Binance ticker response is missing required fields.", ex);
        }
    }

    private static decimal ParseDecimal(JsonElement element, string fieldName)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"Binance field '{fieldName}' has invalid format.");
        }

        if (!decimal.TryParse(
                element.GetString(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal value))
        {
            throw new InvalidOperationException($"Binance field '{fieldName}' is not a valid decimal.");
        }

        return value;
    }

    private static InvalidOperationException CreateBinanceException(
        HttpStatusCode statusCode,
        string content)
    {
        return new InvalidOperationException(
            $"Binance request failed with status code {(int)statusCode} {statusCode}. Response: {content}");
    }

    private static long ToUnixMilliseconds(DateTime utc)
    {
        return new DateTimeOffset(utc).ToUnixTimeMilliseconds();
    }

    private static DateTime FromUnixMilliseconds(long milliseconds)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
    }

    private async Task<HttpResponseMessage> SendAsync(
        string requestUri,
        CancellationToken cancellationToken)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(requestUri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw CreateBinanceException(response.StatusCode, errorContent);
            }

            return response;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("Binance request timed out.");
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Binance request failed due to a network error.", ex);
        }
    }
}