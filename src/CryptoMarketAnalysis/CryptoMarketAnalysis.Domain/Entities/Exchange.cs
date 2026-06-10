using CryptoMarketAnalysis.Domain.Common;
using CryptoMarketAnalysis.Domain.ValueObjects;

namespace CryptoMarketAnalysis.Domain.Entities;

public sealed class Exchange : Entity
{
    public ExchangeCode Code { get; private set; }

    public string Name { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Exchange(
        Guid id,
        ExchangeCode code,
        string name,
        DateTime createdAtUtc) : base(id)
    {
        ArgumentNullException.ThrowIfNull(code);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Exchange name cannot be empty.", nameof(name));

        if (createdAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Created date must be in UTC.", nameof(createdAtUtc));

        Code = code;
        Name = name.Trim();
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public static Exchange Create(string code, string name)
    {
        return new Exchange(
            Guid.NewGuid(),
            new ExchangeCode(code),
            name,
            DateTime.UtcNow);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Exchange name cannot be empty.", nameof(name));

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