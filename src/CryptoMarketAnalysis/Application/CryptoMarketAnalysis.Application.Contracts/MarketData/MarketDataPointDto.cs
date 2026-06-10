namespace CryptoMarketAnalysis.Application.Contracts.MarketData;

public sealed record MarketDataPointDto(
    Guid AssetId,
    Guid ExchangeId,
    DateTime TimestampUtc,
    decimal PriceUsd,
    decimal? MarketCapUsd,
    decimal? Volume24hUsd);