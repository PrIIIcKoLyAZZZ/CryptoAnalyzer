using CryptoMarketAnalysis.Application.Abstractions.Persistence;

namespace CryptoMarketAnalysis.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly CryptoMarketAnalysisDbContext _dbContext;

    public UnitOfWork(CryptoMarketAnalysisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}