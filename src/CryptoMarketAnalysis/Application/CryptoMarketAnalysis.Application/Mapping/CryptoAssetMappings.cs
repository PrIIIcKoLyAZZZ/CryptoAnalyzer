using CryptoMarketAnalysis.Application.Contracts.Assets;
using CryptoMarketAnalysis.Domain.Entities;

namespace CryptoMarketAnalysis.Application.Mapping;

public static class CryptoAssetMappings
{
    public static CryptoAssetDto ToDto(this CryptoAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        return new CryptoAssetDto(
            Id: asset.Id,
            Symbol: asset.Symbol.Value,
            Name: asset.Name,
            IsActive: asset.IsActive);
    }
}