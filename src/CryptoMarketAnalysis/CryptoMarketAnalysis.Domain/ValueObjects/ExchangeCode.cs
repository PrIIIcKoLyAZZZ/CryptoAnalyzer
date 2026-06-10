namespace CryptoMarketAnalysis.Domain.ValueObjects;

public sealed class ExchangeCode : IEquatable<ExchangeCode>
{
    public string Value { get; }

    public ExchangeCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Exchange code cannot be empty.", nameof(value));

        Value = value.Trim().ToUpperInvariant();
    }

    public override string ToString()
    {
        return Value;
    }

    public bool Equals(ExchangeCode? other)
    {
        if (other is null)
            return false;

        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ExchangeCode);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode(StringComparison.Ordinal);
    }

    public static implicit operator string(ExchangeCode code)
    {
        return code.Value;
    }

    public static explicit operator ExchangeCode(string value)
    {
        return new ExchangeCode(value);
    }
}