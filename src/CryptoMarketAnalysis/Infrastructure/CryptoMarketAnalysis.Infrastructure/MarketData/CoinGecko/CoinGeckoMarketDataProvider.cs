using CryptoMarketAnalysis.Application.Abstractions.MarketData;
using CryptoMarketAnalysis.Application.Contracts.MarketData.External;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace CryptoMarketAnalysis.Infrastructure.MarketData.CoinGecko;

public sealed class CoinGeckoMarketDataProvider : IMarketDataProvider, IDisposable
{
    private static readonly Action<ILogger, int, int, Exception?> LogRequestStarted =
        LoggerMessage.Define<int, int>(
            LogLevel.Information,
            new EventId(1, nameof(LogRequestStarted)),
            "CoinGecko request started. Attempt {Attempt}/{MaxAttempts}.");

    private static readonly Action<ILogger, int, int, Exception?> LogRequestSucceeded =
        LoggerMessage.Define<int, int>(
            LogLevel.Information,
            new EventId(2, nameof(LogRequestSucceeded)),
            "CoinGecko request succeeded. Attempt {Attempt}/{MaxAttempts}.");

    private static readonly Action<ILogger, HttpStatusCode, int, int, double, Exception?> LogRetryableStatusCode =
        LoggerMessage.Define<HttpStatusCode, int, int, double>(
            LogLevel.Warning,
            new EventId(3, nameof(LogRetryableStatusCode)),
            "CoinGecko request failed with retryable status code {StatusCode}. Retry attempt {Attempt}/{MaxAttempts} after {Delay} ms.");

    private static readonly Action<ILogger, int, int, double, Exception?> LogCoinGeckoNetworkError =
        LoggerMessage.Define<int, int, double>(
            LogLevel.Warning,
            new EventId(4, nameof(LogCoinGeckoNetworkError)),
            "CoinGecko request failed due to network error. Retry attempt {Attempt}/{MaxAttempts} after {Delay} ms.");

    private static readonly Action<ILogger, int, int, double, Exception?> LogCoinGeckoRequestTimedOut =
        LoggerMessage.Define<int, int, double>(
            LogLevel.Warning,
            new EventId(5, nameof(LogCoinGeckoRequestTimedOut)),
            "CoinGecko request timed out. Retry attempt {Attempt}/{MaxAttempts} after {Delay} ms.");

    private static readonly Action<ILogger, double, Exception?> LogRateLimitWait =
        LoggerMessage.Define<double>(
            LogLevel.Information,
            new EventId(6, nameof(LogRateLimitWait)),
            "CoinGecko rate limit wait: {Delay} ms.");

    private readonly HttpClient _httpClient;
    private readonly CoinGeckoOptions _options;
    private readonly ILogger<CoinGeckoMarketDataProvider> _logger;
    private readonly SemaphoreSlim _requestSemaphore = new(1, 1);

    private DateTimeOffset _lastRequestStartedAt = DateTimeOffset.MinValue;

    public CoinGeckoMarketDataProvider(
        HttpClient httpClient,
        IOptions<CoinGeckoOptions> options,
        ILogger<CoinGeckoMarketDataProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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

        using HttpResponseMessage response = await SendWithRetryAsync(
            requestUri,
            cancellationToken);

        string json = await ReadResponseContentAsync(
            response,
            cancellationToken);

        return CoinGeckoMarketDataMapper.ParseHistoricalResponse(json);
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

        using HttpResponseMessage response = await SendWithRetryAsync(
            requestUri,
            cancellationToken);

        string json = await ReadResponseContentAsync(
            response,
            cancellationToken);

        return CoinGeckoMarketDataMapper.ParseLatestResponse(
            json,
            coinId,
            _options.VsCurrency);
    }

    public void Dispose()
    {
        _requestSemaphore.Dispose();
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
    {
        return statusCode is
            HttpStatusCode.TooManyRequests or
            HttpStatusCode.InternalServerError or
            HttpStatusCode.BadGateway or
            HttpStatusCode.ServiceUnavailable or
            HttpStatusCode.GatewayTimeout;
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

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        string requestUri,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage? lastResponse = null;
        Exception? lastException = null;

        int maxAttempts = Math.Max(1, _options.MaxAttempts);

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            lastResponse?.Dispose();
            lastResponse = null;
            lastException = null;

            try
            {
                await WaitForRateLimitAsync(cancellationToken);

                LogRequestStarted(
                    _logger,
                    attempt,
                    maxAttempts,
                    null);

                HttpResponseMessage response = await _httpClient.GetAsync(
                    requestUri,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    LogRequestSucceeded(
                        _logger,
                        attempt,
                        maxAttempts,
                        null);

                    return response;
                }

                if (!ShouldRetry(response.StatusCode))
                {
                    string statusCode = ((int)response.StatusCode).ToString(
                        CultureInfo.InvariantCulture);

                    string message =
                        $"CoinGecko request failed with status code {statusCode} {response.StatusCode}.";

                    response.Dispose();

                    throw new InvalidOperationException(message);
                }

                lastResponse = response;

                if (attempt == maxAttempts)
                    break;

                TimeSpan delay = GetRetryDelay(
                    response,
                    attempt);

                LogRetryableStatusCode(
                    _logger,
                    response.StatusCode,
                    attempt,
                    maxAttempts,
                    delay.TotalMilliseconds,
                    null);

                await Task.Delay(
                    delay,
                    cancellationToken);
            }
            catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested && attempt < maxAttempts)
            {
                lastException = exception;

                TimeSpan delay = GetRetryDelay(
                    response: null,
                    attempt);

                LogCoinGeckoRequestTimedOut(
                    _logger,
                    attempt,
                    maxAttempts,
                    delay.TotalMilliseconds,
                    exception);

                await Task.Delay(
                    delay,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException exception) when (attempt < maxAttempts)
            {
                lastException = exception;

                TimeSpan delay = GetRetryDelay(
                    response: null,
                    attempt);

                LogCoinGeckoNetworkError(
                    _logger,
                    attempt,
                    maxAttempts,
                    delay.TotalMilliseconds,
                    exception);

                await Task.Delay(
                    delay,
                    cancellationToken);
            }
        }

        if (lastResponse is not null)
        {
            string statusCode = ((int)lastResponse.StatusCode).ToString(
                CultureInfo.InvariantCulture);

            string message =
                $"CoinGecko request failed after {maxAttempts} attempts. Last status code: {statusCode} {lastResponse.StatusCode}.";

            lastResponse.Dispose();

            throw new InvalidOperationException(message);
        }

        throw new InvalidOperationException(
            $"CoinGecko request failed after {maxAttempts} attempts due to network error.",
            lastException);
    }

    private async Task WaitForRateLimitAsync(
        CancellationToken cancellationToken)
    {
        int minIntervalMilliseconds = Math.Max(
            0,
            _options.MinRequestIntervalMilliseconds);

        if (minIntervalMilliseconds == 0)
            return;

        await _requestSemaphore.WaitAsync(cancellationToken);

        try
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            TimeSpan elapsed = now - _lastRequestStartedAt;
            var minInterval = TimeSpan.FromMilliseconds(minIntervalMilliseconds);

            if (elapsed < minInterval)
            {
                TimeSpan delay = minInterval - elapsed;

                LogRateLimitWait(
                    _logger,
                    delay.TotalMilliseconds,
                    null);

                await Task.Delay(
                    delay,
                    cancellationToken);
            }

            _lastRequestStartedAt = DateTimeOffset.UtcNow;
        }
        finally
        {
            _requestSemaphore.Release();
        }
    }

    private TimeSpan GetRetryDelay(
        HttpResponseMessage? response,
        int attempt)
    {
        if (response?.Headers.RetryAfter is not null)
        {
            if (response.Headers.RetryAfter.Delta.HasValue)
                return response.Headers.RetryAfter.Delta.Value;

            if (response.Headers.RetryAfter.Date.HasValue)
            {
                TimeSpan delay = response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;

                if (delay > TimeSpan.Zero)
                    return delay;
            }
        }

        int baseDelaySeconds = Math.Max(
            1,
            _options.BaseRetryDelaySeconds);

        double seconds = baseDelaySeconds * Math.Pow(
            2,
            attempt - 1);

        return TimeSpan.FromSeconds(seconds);
    }
}