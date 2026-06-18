using CryptoMarketAnalysis.Domain.Common;
using CryptoMarketAnalysis.Domain.ValueObjects;

namespace CryptoMarketAnalysis.Domain.Entities;

public sealed class CryptoAsset : Entity
{
    public AssetSymbol Symbol { get; private set; }

    public string Name { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public CryptoAsset(
        Guid id,
        AssetSymbol symbol,
        string name,
        DateTime createdAtUtc) : base(id)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Crypto asset name cannot be empty.", nameof(name));

        if (createdAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Created date must be in UTC.", nameof(createdAtUtc));

        Symbol = symbol;
        Name = name.Trim();
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public static CryptoAsset Create(string symbol, string name)
    {
        return new CryptoAsset(
            Guid.NewGuid(),
            new AssetSymbol(symbol),
            name,
            DateTime.UtcNow);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Crypto asset name cannot be empty.", nameof(name));

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