using CryptoMarketAnalysis.Application.Contracts.MarketData.External;
using System.Globalization;
using System.Text.Json;

namespace CryptoMarketAnalysis.Infrastructure.MarketData.Binance;

internal static class BinanceMarketDataMapper
{
    public static List<ExternalMarketDataPointDto> ParseKlines(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Binance klines response has invalid format.");
            }

            List<ExternalMarketDataPointDto> points = [];

            foreach (JsonElement kline in document.RootElement.EnumerateArray())
            {
                if (kline.ValueKind != JsonValueKind.Array || kline.GetArrayLength() < 8)
                {
                    throw new InvalidOperationException("Binance kline item has invalid format.");
                }

                long openTime = kline[0].GetInt64();
                decimal closePrice = ParseDecimal(kline[4], "close price");
                decimal quoteVolume = ParseDecimal(kline[7], "quote asset volume");

                if (closePrice <= 0)
                {
                    throw new InvalidOperationException("Binance close price must be greater than zero.");
                }

                if (quoteVolume < 0)
                {
                    throw new InvalidOperationException("Binance quote volume must not be negative.");
                }

                points.Add(new ExternalMarketDataPointDto(
                    FromUnixMilliseconds(openTime),
                    closePrice,
                    null,
                    quoteVolume));
            }

            return points;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Binance klines response contains invalid JSON.", ex);
        }
    }

    public static ExternalMarketDataPointDto ParseLatest(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            JsonElement root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("Binance ticker response has invalid format.");
            }

            decimal lastPrice = ParseDecimal(root.GetProperty("lastPrice"), "lastPrice");
            decimal quoteVolume = ParseDecimal(root.GetProperty("quoteVolume"), "quoteVolume");
            long closeTime = root.GetProperty("closeTime").GetInt64();

            if (lastPrice <= 0)
            {
                throw new InvalidOperationException("Binance lastPrice must be greater than zero.");
            }

            if (quoteVolume < 0)
            {
                throw new InvalidOperationException("Binance quoteVolume must not be negative.");
            }

            return new ExternalMarketDataPointDto(
                FromUnixMilliseconds(closeTime),
                lastPrice,
                null,
                quoteVolume);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Binance ticker response contains invalid JSON.", ex);
        }
        catch (KeyNotFoundException ex)
        {
            throw new InvalidOperationException("Binance ticker response is missing required fields.", ex);
        }
    }

    private static decimal ParseDecimal(JsonElement element, string fieldName)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"Binance field '{fieldName}' has invalid format.");
        }

        if (!decimal.TryParse(
                element.GetString(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal value))
        {
            throw new InvalidOperationException($"Binance field '{fieldName}' is not a valid decimal.");
        }

        return value;
    }

    private static DateTime FromUnixMilliseconds(long milliseconds)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
    }
}