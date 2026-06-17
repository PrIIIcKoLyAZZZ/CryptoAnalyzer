using CryptoMarketAnalysis.Domain.Entities;
using CryptoMarketAnalysis.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CryptoMarketAnalysis.Infrastructure.Persistence.Configurations;

public sealed class ExchangeConfiguration : IEntityTypeConfiguration<Exchange>
{
    public void Configure(EntityTypeBuilder<Exchange> builder)
    {
        builder.ToTable("exchanges");

        builder.HasKey(exchange => exchange.Id);

        builder.Property(exchange => exchange.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(exchange => exchange.Code)
            .HasColumnName("code")
            .HasConversion(
                code => code.Value,
                value => new ExchangeCode(value))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(exchange => exchange.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(exchange => exchange.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(exchange => exchange.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(exchange => exchange.Code)
            .HasDatabaseName("ix_exchanges_code")
            .IsUnique();
    }
}