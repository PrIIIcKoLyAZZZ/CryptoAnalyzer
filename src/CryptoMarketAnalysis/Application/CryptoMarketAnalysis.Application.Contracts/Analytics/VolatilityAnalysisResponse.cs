namespace CryptoMarketAnalysis.Application.Contracts.Analytics;

public sealed record VolatilityAnalysisResponse(
    string Symbol,
    string? MarketDataSourceCode,
    DateTime FromUtc,
    DateTime ToUtc,
    int PointsCount,
    int ReturnsCount,
    decimal? AverageReturnPercent,
    decimal? VolatilityPercent);