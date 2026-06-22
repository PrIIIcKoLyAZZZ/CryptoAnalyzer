namespace CryptoMarketAnalysis.Application.Contracts.Reports;

public sealed record MarketAnalysisReportPointDto(
    DateTime TimestampUtc,
    decimal PriceUsd,
    decimal? MarketCapUsd,
    decimal? Volume24hUsd);