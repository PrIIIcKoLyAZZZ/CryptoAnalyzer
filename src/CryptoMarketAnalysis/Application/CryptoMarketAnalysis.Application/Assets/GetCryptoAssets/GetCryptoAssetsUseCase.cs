using CryptoMarketAnalysis.Application.Abstractions.Persistence;
using CryptoMarketAnalysis.Application.Contracts.Assets;
using CryptoMarketAnalysis.Application.Mapping;
using CryptoMarketAnalysis.Domain.Entities;

namespace CryptoMarketAnalysis.Application.Assets.GetCryptoAssets;

public class GetCryptoAssetsUseCase : IGetCryptoAssetsUseCase
{
    private readonly ICryptoAssetRepository _cryptoAssetRepository;

    public GetCryptoAssetsUseCase(ICryptoAssetRepository cryptoAssetRepository)
    {
        _cryptoAssetRepository = cryptoAssetRepository
            ?? throw new ArgumentNullException(nameof(cryptoAssetRepository));
    }

    public async Task<IReadOnlyCollection<CryptoAssetDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<CryptoAsset> cryptoAssets = await _cryptoAssetRepository.GetActiveAsync(cancellationToken);

        return cryptoAssets
            .OrderBy(cryptoAsset => cryptoAsset.Symbol.Value, StringComparer.Ordinal)
            .Select(cryptoAsset => cryptoAsset.ToDto())
            .ToList();
    }
}