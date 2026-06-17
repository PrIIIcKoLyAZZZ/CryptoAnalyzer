using CryptoMarketAnalysis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoMarketAnalysis.Infrastructure.Persistence.Configurations;

public sealed class MarketDataPointConfiguration : IEntityTypeConfiguration<MarketDataPoint>
{
    public void Configure(EntityTypeBuilder<MarketDataPoint> builder)
    {
        builder.ToTable("market_data_points");

        builder.HasKey(point => point.Id);

        builder.Property(point => point.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(point => point.AssetId)
            .HasColumnName("asset_id")
            .IsRequired();

        builder.Property(point => point.ExchangeId)
            .HasColumnName("exchange_id")
            .IsRequired();

        builder.Property(point => point.TimestampUtc)
            .HasColumnName("timestamp_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(point => point.PriceUsd)
            .HasColumnName("price_usd")
            .HasColumnType("numeric(28, 10)")
            .IsRequired();

        builder.Property(point => point.MarketCapUsd)
            .HasColumnName("market_cap_usd")
            .HasColumnType("numeric(28, 2)");

        builder.Property(point => point.Volume24hUsd)
            .HasColumnName("volume_24h_usd")
            .HasColumnType("numeric(28, 2)");

        builder.Property(point => point.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(point => new
            {
                point.AssetId,
                point.TimestampUtc,
            })
            .HasDatabaseName("ix_market_data_points_asset_id_timestamp_utc");

        builder.HasIndex(point => new
            {
                point.ExchangeId,
                point.TimestampUtc,
            })
            .HasDatabaseName("ix_market_data_points_exchange_id_timestamp_utc");

        builder.HasIndex(point => new
            {
                point.AssetId,
                point.ExchangeId,
                point.TimestampUtc,
            })
            .HasDatabaseName("ux_market_data_points_asset_id_exchange_id_timestamp_utc")
            .IsUnique();

        builder.HasOne<CryptoAsset>()
            .WithMany()
            .HasForeignKey(point => point.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Exchange>()
            .WithMany()
            .HasForeignKey(point => point.ExchangeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}