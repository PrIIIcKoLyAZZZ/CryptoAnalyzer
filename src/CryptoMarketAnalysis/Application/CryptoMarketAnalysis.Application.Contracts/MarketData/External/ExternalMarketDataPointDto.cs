namespace CryptoMarketAnalysis.Application.Contracts.MarketData.External;

public sealed record ExternalMarketDataPointDto(
    DateTime TimestampUtc,
    decimal PriceUsd,
    decimal? MarketCapUsd,
    decimal? Volume24hUsd);