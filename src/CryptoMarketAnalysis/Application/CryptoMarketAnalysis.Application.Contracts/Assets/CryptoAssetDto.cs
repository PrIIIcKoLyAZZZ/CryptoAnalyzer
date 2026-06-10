namespace CryptoMarketAnalysis.Application.Contracts.Assets;

public sealed record CryptoAssetDto(
    Guid Id,
    string Symbol,
    string Name,
    bool IsActive);