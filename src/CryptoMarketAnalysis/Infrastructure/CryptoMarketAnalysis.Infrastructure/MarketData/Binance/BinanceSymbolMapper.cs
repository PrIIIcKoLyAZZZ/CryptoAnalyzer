namespace CryptoMarketAnalysis.Infrastructure.MarketData.Binance;

public sealed class BinanceSymbolMapper
{
    public string MapToBinanceSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new InvalidOperationException("Binance symbol must not be empty.");
        }

        return symbol.Trim().ToUpperInvariant() switch
        {
            "BTC" => "BTCUSDT",
            "ETH" => "ETHUSDT",
            _ => throw new InvalidOperationException($"Unsupported Binance symbol '{symbol}'."),
        };
    }
}