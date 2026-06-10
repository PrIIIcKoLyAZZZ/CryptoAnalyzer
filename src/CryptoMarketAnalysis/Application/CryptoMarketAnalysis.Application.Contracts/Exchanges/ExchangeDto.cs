namespace CryptoMarketAnalysis.Application.Contracts.Exchanges;

public sealed record ExchangeDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive);