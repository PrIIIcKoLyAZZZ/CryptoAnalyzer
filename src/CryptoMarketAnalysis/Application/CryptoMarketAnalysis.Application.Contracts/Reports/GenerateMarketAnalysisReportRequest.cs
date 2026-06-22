namespace CryptoMarketAnalysis.Application.Contracts.Reports;

public sealed record GenerateMarketAnalysisReportRequest(
    string Symbol,
    DateTime FromUtc,
    DateTime ToUtc,
    string? MarketDataSourceCode = null,
    string? CorrelationSymbol = null,
    string? OutputPath = null);