namespace CryptoMarketAnalysis.Infrastructure.MarketData.Binance;

public sealed class BinanceOptions
{
    public string BaseUrl { get; init; } = "https://api.binance.com";

    public string Interval { get; init; } = "1d";

    public int Limit { get; init; } = 1000;
}