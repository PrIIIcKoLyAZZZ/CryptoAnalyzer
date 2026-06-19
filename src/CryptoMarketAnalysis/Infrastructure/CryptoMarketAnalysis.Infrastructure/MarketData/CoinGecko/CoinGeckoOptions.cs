namespace CryptoMarketAnalysis.Infrastructure.MarketData.CoinGecko;

public sealed class CoinGeckoOptions
{
    public string BaseUrl { get; init; } = "https://api.coingecko.com/api/v3";

    public string? DemoApiKey { get; init; }

    public string VsCurrency { get; init; } = "usd";

    public string Interval { get; init; } = "daily";

    public int MaxAttempts { get; init; } = 3;

    public int BaseRetryDelaySeconds { get; init; } = 1;

    public int MinRequestIntervalMilliseconds { get; init; } = 800;
}