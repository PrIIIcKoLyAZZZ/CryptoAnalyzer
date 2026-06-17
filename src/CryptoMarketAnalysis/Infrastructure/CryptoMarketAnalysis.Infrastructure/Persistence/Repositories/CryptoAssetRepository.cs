using CryptoMarketAnalysis.Application.Abstractions.Persistence;
using CryptoMarketAnalysis.Domain.Entities;
using CryptoMarketAnalysis.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace CryptoMarketAnalysis.Infrastructure.Persistence.Repositories;

public sealed class CryptoAssetRepository : ICryptoAssetRepository
{
    private readonly CryptoMarketAnalysisDbContext _dbContext;

    public CryptoAssetRepository(CryptoMarketAnalysisDbContext dbContext)
    {
        _dbContext = dbContext
            ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public Task<CryptoAsset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.CryptoAssets
            .FirstOrDefaultAsync(cryptoAsset => cryptoAsset.Id == id, cancellationToken);
    }

    public Task<CryptoAsset?> GetBySymbolAsync(AssetSymbol symbol, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        return _dbContext.CryptoAssets
            .FirstOrDefaultAsync(cryptoAsset => cryptoAsset.Symbol == symbol, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CryptoAsset>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.CryptoAssets
            .Where(cryptoAsset => cryptoAsset.IsActive)
            .OrderBy(cryptoAsset => cryptoAsset.Symbol)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CryptoAsset asset, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(asset);
        await _dbContext.CryptoAssets.AddAsync(asset, cancellationToken);
    }
}