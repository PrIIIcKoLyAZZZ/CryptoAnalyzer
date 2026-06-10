using CryptoMarketAnalysis.Domain.Entities;
using CryptoMarketAnalysis.Domain.ValueObjects;

namespace CryptoMarketAnalysis.Application.Abstractions.Persistence;

public interface ICryptoAssetRepository
{
    Task<CryptoAsset?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CryptoAsset?> GetBySymbolAsync(
        AssetSymbol symbol,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CryptoAsset>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CryptoAsset asset,
        CancellationToken cancellationToken = default);
}