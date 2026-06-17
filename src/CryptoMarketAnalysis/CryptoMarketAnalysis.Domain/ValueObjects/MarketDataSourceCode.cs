namespace CryptoMarketAnalysis.Domain.ValueObjects;

public sealed class MarketDataSourceCode : IEquatable<MarketDataSourceCode>
{
    public string Value { get; }

    public MarketDataSourceCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Market data source code cannot be empty.", nameof(value));

        Value = value.Trim().ToUpperInvariant();
    }

    public override string ToString()
    {
        return Value;
    }

    public bool Equals(MarketDataSourceCode? other)
    {
        if (other is null)
            return false;

        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as MarketDataSourceCode);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode(StringComparison.Ordinal);
    }

    public static implicit operator string(MarketDataSourceCode code)
    {
        return code.Value;
    }

    public static explicit operator MarketDataSourceCode(string value)
    {
        return new MarketDataSourceCode(value);
    }
}