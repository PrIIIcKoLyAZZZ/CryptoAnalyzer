using CryptoMarketAnalysis.Application.Abstractions.Persistence;
using CryptoMarketAnalysis.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CryptoMarketAnalysis.Infrastructure.Persistence.Repositories;

public sealed class MarketDataRepository : IMarketDataRepository
{
    private readonly CryptoMarketAnalysisDbContext _dbContext;

    public MarketDataRepository(CryptoMarketAnalysisDbContext dbContext)
    {
        _dbContext = dbContext
            ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyCollection<MarketDataPoint>> GetHistoricalAsync(
        Guid assetId,
        DateTime fromUtc,
        DateTime toUtc,
        Guid? exchangeId = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<MarketDataPoint> query = _dbContext.MarketDataPoints
            .Where(point =>
                point.AssetId == assetId &&
                point.TimestampUtc >= fromUtc &&
                point.TimestampUtc <= toUtc);

        if (exchangeId.HasValue)
            query = query.Where(point => point.ExchangeId == exchangeId.Value);

        return await query
            .OrderBy(point => point.TimestampUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<MarketDataPoint?> GetLatestAsync(Guid assetId, Guid? exchangeId = null, CancellationToken cancellationToken = default)
    {
        IQueryable<MarketDataPoint> query = _dbContext.MarketDataPoints
            .Where(point => point.AssetId == assetId);

        if (exchangeId.HasValue)
            query = query.Where(point => point.ExchangeId == exchangeId.Value);

        return query
            .OrderByDescending(point => point.TimestampUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid assetId, Guid exchangeId, DateTime timestampUtc, CancellationToken cancellationToken = default)
    {
        return _dbContext.MarketDataPoints.AnyAsync(
            point =>
                point.AssetId == assetId &&
                point.ExchangeId == exchangeId &&
                point.TimestampUtc == timestampUtc,
            cancellationToken);
    }

    public async Task AddAsync(MarketDataPoint marketDataPoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(marketDataPoint);
        await _dbContext.MarketDataPoints.AddAsync(marketDataPoint, cancellationToken);
    }

    public async Task AddRangeAsync(IReadOnlyCollection<MarketDataPoint> marketDataPoints, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(marketDataPoints);
        if (marketDataPoints.Count == 0) return;
        await _dbContext.MarketDataPoints.AddRangeAsync(marketDataPoints, cancellationToken);
    }
}