namespace CryptoMarketAnalysis.Application.Contracts.Assets;

public interface IGetCryptoAssetsUseCase
{
    Task<IReadOnlyCollection<CryptoAssetDto>> ExecuteAsync(
        CancellationToken cancellationToken = default);
}