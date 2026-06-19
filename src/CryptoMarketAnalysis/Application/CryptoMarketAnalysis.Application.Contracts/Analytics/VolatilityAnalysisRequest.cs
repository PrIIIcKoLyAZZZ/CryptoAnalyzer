namespace CryptoMarketAnalysis.Application.Contracts.Analytics;

public sealed record VolatilityAnalysisRequest(
    string Symbol,
    DateTime FromUtc,
    DateTime ToUtc,
    string? MarketDataSourceCode = null);