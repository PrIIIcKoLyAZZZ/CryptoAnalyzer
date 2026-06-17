using CryptoMarketAnalysis.Domain.Common;
using CryptoMarketAnalysis.Domain.ValueObjects;

namespace CryptoMarketAnalysis.Domain.Entities;

public sealed class MarketDataSource : Entity
{
    public MarketDataSourceCode Code { get; private set; }

    public string Name { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public MarketDataSource(
        Guid id,
        MarketDataSourceCode code,
        string name,
        DateTime createdAtUtc) : base(id)
    {
        ArgumentNullException.ThrowIfNull(code);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Market data source name cannot be empty.", nameof(name));

        if (createdAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Created date must be in UTC.", nameof(createdAtUtc));

        Code = code;
        Name = name.Trim();
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public static MarketDataSource Create(string code, string name)
    {
        return new MarketDataSource(
            Guid.NewGuid(),
            new MarketDataSourceCode(code),
            name,
            DateTime.UtcNow);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Market data source name cannot be empty.", nameof(name));

        Name = name.Trim();
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}