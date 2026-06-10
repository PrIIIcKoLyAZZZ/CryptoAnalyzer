namespace CryptoMarketAnalysis.Application.Contracts.MarketData;

public sealed record HistoricalMarketDataRequest(
    string Symbol,
    DateTime FromUtc,
    DateTime ToUtc,
    string? ExchangeCode = null);