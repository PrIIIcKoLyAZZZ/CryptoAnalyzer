using CryptoMarketAnalysis.Domain.Entities;
using CryptoMarketAnalysis.Domain.ValueObjects;

namespace CryptoMarketAnalysis.Application.Abstractions.Persistence;

public interface IMarketDataSourceRepository
{
    Task<MarketDataSource?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<MarketDataSource?> GetByCodeAsync(
        MarketDataSourceCode code,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MarketDataSource>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MarketDataSource marketDataSource,
        CancellationToken cancellationToken = default);
}