namespace CryptoMarketAnalysis.Application.Contracts.MarketData.Loading;

public sealed record LoadMarketDataResponse(
    LoadMarketDataStatus Status,
    int RequestedSymbolsCount,
    int LoadedPointsCount,
    int SkippedDuplicatesCount,
    IReadOnlyCollection<LoadMarketDataSymbolResult> Results);