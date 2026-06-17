using CryptoMarketAnalysis.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CryptoMarketAnalysis.Infrastructure.Persistence;

public sealed class CryptoMarketAnalysisDbContext : DbContext
{
    public CryptoMarketAnalysisDbContext(
        DbContextOptions<CryptoMarketAnalysisDbContext> options)
        : base(options)
    {
    }

    public DbSet<CryptoAsset> CryptoAssets => Set<CryptoAsset>();

    public DbSet<MarketDataSource> MarketDataSources => Set<MarketDataSource>();

    public DbSet<MarketDataPoint> MarketDataPoints => Set<MarketDataPoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CryptoMarketAnalysisDbContext).Assembly);
    }
}