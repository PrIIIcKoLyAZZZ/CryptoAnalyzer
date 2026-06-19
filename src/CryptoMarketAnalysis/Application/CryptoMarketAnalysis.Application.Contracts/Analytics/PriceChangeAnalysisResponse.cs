namespace CryptoMarketAnalysis.Application.Contracts.Analytics;

public sealed record PriceChangeAnalysisResponse(
    string Symbol,
    string? MarketDataSourceCode,
    DateTime FromUtc,
    DateTime ToUtc,
    decimal? StartPriceUsd,
    decimal? EndPriceUsd,
    decimal? AbsoluteChangeUsd,
    decimal? PercentageChange,
    int PointsCount);