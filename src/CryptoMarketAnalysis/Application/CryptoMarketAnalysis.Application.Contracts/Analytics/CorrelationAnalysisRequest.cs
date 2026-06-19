namespace CryptoMarketAnalysis.Application.Contracts.Analytics;

public sealed record CorrelationAnalysisRequest(
    string BaseSymbol,
    string QuoteSymbol,
    DateTime FromUtc,
    DateTime ToUtc,
    string? MarketDataSourceCode = null);