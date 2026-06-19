namespace CryptoMarketAnalysis.Application.Contracts.Analytics;

public sealed record PriceChangeAnalysisRequest(
    string Symbol,
    DateTime FromUtc,
    DateTime ToUtc,
    string? MarketDataSourceCode = null);