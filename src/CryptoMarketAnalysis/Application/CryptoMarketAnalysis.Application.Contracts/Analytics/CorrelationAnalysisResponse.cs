namespace CryptoMarketAnalysis.Application.Contracts.Analytics;

public sealed record CorrelationAnalysisResponse(
    string BaseSymbol,
    string QuoteSymbol,
    string? MarketDataSourceCode,
    DateTime FromUtc,
    DateTime ToUtc,
    int BasePointsCount,
    int QuotePointsCount,
    int MatchedReturnsCount,
    decimal? PearsonCorrelation);