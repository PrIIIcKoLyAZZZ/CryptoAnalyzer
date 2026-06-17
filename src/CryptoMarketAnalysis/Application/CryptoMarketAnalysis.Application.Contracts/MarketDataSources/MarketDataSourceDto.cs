namespace CryptoMarketAnalysis.Application.Contracts.MarketDataSources;

public sealed record MarketDataSourceDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive);