using CryptoMarketAnalysis.Application.Abstractions.Persistence;
using CryptoMarketAnalysis.Domain.Entities;
using CryptoMarketAnalysis.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace CryptoMarketAnalysis.Infrastructure.Persistence.Repositories;

public sealed class MarketDataSourceRepository : IMarketDataSourceRepository
{
    private readonly CryptoMarketAnalysisDbContext _dbContext;

    public MarketDataSourceRepository(CryptoMarketAnalysisDbContext dbContext)
    {
        _dbContext = dbContext
                     ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public Task<MarketDataSource?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.MarketDataSources
            .FirstOrDefaultAsync(
                marketDataSource => marketDataSource.Id == id,
                cancellationToken);
    }

    public Task<MarketDataSource?> GetByCodeAsync(
        MarketDataSourceCode code,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);

        return _dbContext.MarketDataSources
            .FirstOrDefaultAsync(
                marketDataSource => marketDataSource.Code == code,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<MarketDataSource>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.MarketDataSources
            .Where(marketDataSource => marketDataSource.IsActive)
            .OrderBy(marketDataSource => marketDataSource.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        MarketDataSource marketDataSource,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(marketDataSource);

        await _dbContext.MarketDataSources.AddAsync(
            marketDataSource,
            cancellationToken);
    }
}