using CryptoMarketAnalysis.Domain.Entities;
using CryptoMarketAnalysis.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoMarketAnalysis.Infrastructure.Persistence.Configurations;

public sealed class MarketDataSourceConfiguration : IEntityTypeConfiguration<MarketDataSource>
{
    public void Configure(EntityTypeBuilder<MarketDataSource> builder)
    {
        builder.ToTable("market_data_sources");

        builder.HasKey(marketDataSource => marketDataSource.Id);

        builder.Property(marketDataSource => marketDataSource.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(marketDataSource => marketDataSource.Code)
            .HasColumnName("code")
            .HasConversion(
                code => code.Value,
                value => new MarketDataSourceCode(value))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(marketDataSource => marketDataSource.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(marketDataSource => marketDataSource.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(marketDataSource => marketDataSource.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(marketDataSource => marketDataSource.Code)
            .HasDatabaseName("ix_market_data_sources_code")
            .IsUnique();
    }
}