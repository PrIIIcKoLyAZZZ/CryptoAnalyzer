using CryptoMarketAnalysis.Domain.Entities;
using CryptoMarketAnalysis.Domain.ValueObjects;

namespace CryptoMarketAnalysis.Application.Abstractions.Persistence;

public interface IExchangeRepository
{
    Task<Exchange?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Exchange?> GetByCodeAsync(
        ExchangeCode code,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Exchange>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Exchange exchange,
        CancellationToken cancellationToken = default);
}