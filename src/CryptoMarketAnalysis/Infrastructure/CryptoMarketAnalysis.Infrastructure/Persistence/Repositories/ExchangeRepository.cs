using CryptoMarketAnalysis.Application.Abstractions.Persistence;
using CryptoMarketAnalysis.Domain.Entities;
using CryptoMarketAnalysis.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace CryptoMarketAnalysis.Infrastructure.Persistence.Repositories;

public sealed class ExchangeRepository : IExchangeRepository
{
    private readonly CryptoMarketAnalysisDbContext _dbContext;

    public ExchangeRepository(CryptoMarketAnalysisDbContext dbContext)
    {
        _dbContext = dbContext
            ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public Task<Exchange?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Exchanges.FirstOrDefaultAsync(exchange => exchange.Id == id, cancellationToken);
    }

    public Task<Exchange?> GetByCodeAsync(ExchangeCode code, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);
        return _dbContext.Exchanges.FirstOrDefaultAsync(exchange => exchange.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Exchange>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Exchanges
            .Where(exchange => exchange.IsActive)
            .OrderBy(exchange => exchange.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Exchange exchange, CancellationToken cancellationToken = default)
    {
        await _dbContext.Exchanges.AddAsync(exchange, cancellationToken);
    }
}