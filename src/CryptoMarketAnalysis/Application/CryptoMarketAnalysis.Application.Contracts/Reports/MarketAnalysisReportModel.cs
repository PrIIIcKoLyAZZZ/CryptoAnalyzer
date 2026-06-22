using CryptoMarketAnalysis.Application.Contracts.Analytics;

namespace CryptoMarketAnalysis.Application.Contracts.Reports;

public sealed record MarketAnalysisReportModel(
    string Symbol,
    string? MarketDataSourceCode,
    DateTime FromUtc,
    DateTime ToUtc,
    DateTime GeneratedAtUtc,
    IReadOnlyCollection<MarketAnalysisReportPointDto> Points,
    PriceChangeAnalysisResponse PriceChange,
    VolatilityAnalysisResponse Volatility,
    CorrelationAnalysisResponse? Correlation);