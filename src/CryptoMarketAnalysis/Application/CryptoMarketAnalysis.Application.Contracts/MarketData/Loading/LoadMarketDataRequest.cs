namespace CryptoMarketAnalysis.Application.Contracts.MarketData.Loading;

public sealed record LoadMarketDataRequest(
    IReadOnlyCollection<string> Symbols,
    DateTime FromUtc,
    DateTime ToUtc,
    string? MarketDataSourceCode = null);