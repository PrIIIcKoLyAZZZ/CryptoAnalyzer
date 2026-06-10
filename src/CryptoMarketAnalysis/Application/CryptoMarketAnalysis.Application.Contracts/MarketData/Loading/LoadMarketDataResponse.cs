namespace CryptoMarketAnalysis.Application.Contracts.MarketData.Loading;

public sealed record LoadMarketDataResponse(
    int RequestedSymbolsCount,
    int LoadedPointsCount,
    int SkippedDuplicatesCount,
    IReadOnlyCollection<LoadMarketDataSymbolResult> Results);