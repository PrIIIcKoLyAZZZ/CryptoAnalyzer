namespace CryptoMarketAnalysis.Domain.ValueObjects;

public sealed class AssetSymbol : IEquatable<AssetSymbol>
{
    public string Value { get; }

    public AssetSymbol(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Asset symbol cannot be empty.", nameof(value));

        Value = value.Trim().ToUpperInvariant();
    }

    public override string ToString()
    {
        return Value;
    }

    public bool Equals(AssetSymbol? other)
    {
        if (other is null)
            return false;

        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as AssetSymbol);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode(StringComparison.Ordinal);
    }

    public static implicit operator string(AssetSymbol symbol)
    {
        return symbol.Value;
    }

    public static explicit operator AssetSymbol(string value)
    {
        return new AssetSymbol(value);
    }
}