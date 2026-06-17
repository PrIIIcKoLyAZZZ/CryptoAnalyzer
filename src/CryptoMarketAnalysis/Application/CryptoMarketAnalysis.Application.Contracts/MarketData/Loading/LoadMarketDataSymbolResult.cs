namespace CryptoMarketAnalysis.Application.Contracts.MarketData.Loading;

public sealed record LoadMarketDataSymbolResult(
    string Symbol,
    string MarketDataSourceCode,
    int LoadedPointsCount,
    int SkippedDuplicatesCount,
    string? Error);