using CryptoMarketAnalysis.Domain.Entities;
using CryptoMarketAnalysis.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoMarketAnalysis.Infrastructure.Persistence.Configurations;

public sealed class CryptoAssetConfiguration : IEntityTypeConfiguration<CryptoAsset>
{
    public void Configure(EntityTypeBuilder<CryptoAsset> builder)
    {
        builder.ToTable("crypto_assets");

        builder.HasKey(asset => asset.Id);

        builder.Property(asset => asset.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(asset => asset.Symbol)
            .HasColumnName("symbol")
            .HasConversion(
                symbol => symbol.Value,
                value => new AssetSymbol(value))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(asset => asset.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(asset => asset.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(asset => asset.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(asset => asset.Symbol)
            .HasDatabaseName("ix_crypto_assets_symbol")
            .IsUnique();
    }
}