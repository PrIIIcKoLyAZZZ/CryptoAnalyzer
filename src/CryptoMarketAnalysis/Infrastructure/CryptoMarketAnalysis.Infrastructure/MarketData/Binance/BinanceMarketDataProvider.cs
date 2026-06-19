using CryptoMarketAnalysis.Application.Abstractions.MarketData;
using CryptoMarketAnalysis.Application.Contracts.MarketData.External;
using Microsoft.Extensions.Options;
using System.Net;

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

            List<ExternalMarketDataPointDto> batch = BinanceMarketDataMapper.ParseKlines(content);

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

        return BinanceMarketDataMapper.ParseLatest(content);
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