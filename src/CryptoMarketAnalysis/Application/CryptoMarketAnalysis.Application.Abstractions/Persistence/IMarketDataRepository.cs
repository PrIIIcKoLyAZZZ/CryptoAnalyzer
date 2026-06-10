using CryptoMarketAnalysis.Domain.Entities;

namespace CryptoMarketAnalysis.Application.Abstractions.Persistence;

public interface IMarketDataRepository
{
    Task<IReadOnlyCollection<MarketDataPoint>> GetHistoricalAsync(
        Guid assetId,
        DateTime fromUtc,
        DateTime toUtc,
        Guid? exchangeId = null,
        CancellationToken cancellationToken = default);

    Task<MarketDataPoint?> GetLatestAsync(
        Guid assetId,
        Guid? exchangeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid assetId,
        Guid exchangeId,
        DateTime timestampUtc,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MarketDataPoint marketDataPoint,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IReadOnlyCollection<MarketDataPoint> marketDataPoints,
        CancellationToken cancellationToken = default);
}