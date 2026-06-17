namespace CryptoMarketAnalysis.Application.Contracts.MarketData;

public sealed record HistoricalMarketDataResponse(
    string Symbol,
    string? MarketDataSourceCode,
    IReadOnlyCollection<MarketDataPointDto> Points);