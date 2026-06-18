namespace CryptoMarketAnalysis.Infrastructure.MarketData.CoinGecko;

public static class CoinGeckoSymbolMapper
{
    public static string ToCoinId(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be empty.", nameof(symbol));

        return symbol.Trim().ToUpperInvariant() switch
        {
            "BTC" => "bitcoin",
            "ETH" => "ethereum",
            _ => throw new ArgumentException(
                $"CoinGecko mapping for symbol '{symbol}' was not found."),
        };
    }
}