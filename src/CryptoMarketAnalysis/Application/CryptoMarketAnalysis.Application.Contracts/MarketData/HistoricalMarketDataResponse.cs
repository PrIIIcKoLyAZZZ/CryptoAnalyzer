namespace CryptoMarketAnalysis.Application.Contracts.MarketData;

public sealed record HistoricalMarketDataResponse(
    string Symbol,
    string? ExchangeCode,
    IReadOnlyCollection<MarketDataPointDto> Points);