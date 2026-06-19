using CryptoMarketAnalysis.Application.Contracts.MarketData.External;
using System.Globalization;
using System.Text.Json;

namespace CryptoMarketAnalysis.Infrastructure.MarketData.CoinGecko;

internal static class CoinGeckoMarketDataMapper
{
    public static List<ExternalMarketDataPointDto> ParseHistoricalResponse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);

            JsonElement root = document.RootElement;

            Dictionary<long, decimal> prices = ReadTimeSeries(
                root,
                "prices");

            Dictionary<long, decimal> marketCaps = ReadTimeSeries(
                root,
                "market_caps");

            Dictionary<long, decimal> volumes = ReadTimeSeries(
                root,
                "total_volumes");

            return prices
                .OrderBy(pair => pair.Key)
                .Select(pair =>
                {
                    DateTime timestampUtc = DateTimeOffset
                        .FromUnixTimeMilliseconds(pair.Key)
                        .UtcDateTime;

                    marketCaps.TryGetValue(pair.Key, out decimal marketCapUsd);
                    volumes.TryGetValue(pair.Key, out decimal volume24hUsd);

                    return new ExternalMarketDataPointDto(
                        timestampUtc,
                        pair.Value,
                        marketCaps.ContainsKey(pair.Key) ? marketCapUsd : null,
                        volumes.ContainsKey(pair.Key) ? volume24hUsd : null);
                })
                .ToList();
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "CoinGecko returned invalid historical JSON response.",
                exception);
        }
    }

    public static ExternalMarketDataPointDto? ParseLatestResponse(
        string json,
        string coinId,
        string vsCurrency)
    {
        try
        {
            using var document = JsonDocument.Parse(json);

            JsonElement root = document.RootElement;

            if (!root.TryGetProperty(coinId, out JsonElement coinElement))
                return null;

            string currency = vsCurrency.ToLowerInvariant();
            string marketCapProperty = $"{currency}_market_cap";
            string volumeProperty = $"{currency}_24h_vol";

            if (!TryGetDecimal(coinElement, currency, out decimal priceUsd))
                return null;

            decimal? marketCapUsd = TryGetDecimal(
                coinElement,
                marketCapProperty,
                out decimal marketCap)
                    ? marketCap
                    : null;

            decimal? volume24hUsd = TryGetDecimal(
                coinElement,
                volumeProperty,
                out decimal volume)
                    ? volume
                    : null;

            if (!TryGetInt64(coinElement, "last_updated_at", out long lastUpdatedAt))
                throw new InvalidOperationException("CoinGecko latest response does not contain last_updated_at.");

            DateTime timestampUtc = DateTimeOffset
                .FromUnixTimeSeconds(lastUpdatedAt)
                .UtcDateTime;

            return new ExternalMarketDataPointDto(
                timestampUtc,
                priceUsd,
                marketCapUsd,
                volume24hUsd);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "CoinGecko returned invalid latest JSON response.",
                exception);
        }
    }

    private static Dictionary<long, decimal> ReadTimeSeries(
        JsonElement root,
        string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement seriesElement))
            return new Dictionary<long, decimal>();

        if (seriesElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"CoinGecko field '{propertyName}' has invalid format.");

        var result = new Dictionary<long, decimal>();

        foreach (JsonElement item in seriesElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Array || item.GetArrayLength() < 2)
                continue;

            JsonElement timestampElement = item[0];
            JsonElement valueElement = item[1];

            if (!TryGetInt64(timestampElement, out long timestampMilliseconds))
                continue;

            if (!TryGetDecimal(valueElement, out decimal value))
                continue;

            result[timestampMilliseconds] = value;
        }

        return result;
    }

    private static bool TryGetDecimal(
        JsonElement element,
        string propertyName,
        out decimal value)
    {
        value = default;

        if (!element.TryGetProperty(propertyName, out JsonElement property))
            return false;

        return TryGetDecimal(
            property,
            out value);
    }

    private static bool TryGetDecimal(
        JsonElement element,
        out decimal value)
    {
        value = default;

        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetDecimal(out value),
            JsonValueKind.String => decimal.TryParse(
                element.GetString(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out value),
            JsonValueKind.Undefined => false,
            JsonValueKind.Object => false,
            JsonValueKind.Array => false,
            JsonValueKind.True => false,
            JsonValueKind.False => false,
            JsonValueKind.Null => false,
            _ => false,
        };
    }

    private static bool TryGetInt64(
        JsonElement element,
        string propertyName,
        out long value)
    {
        value = default;

        if (!element.TryGetProperty(propertyName, out JsonElement property))
            return false;

        return TryGetInt64(
            property,
            out value);
    }

    private static bool TryGetInt64(
        JsonElement element,
        out long value)
    {
        value = default;

        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt64(out value),
            JsonValueKind.String => long.TryParse(
                element.GetString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value),
            JsonValueKind.Undefined => false,
            JsonValueKind.Object => false,
            JsonValueKind.Array => false,
            JsonValueKind.True => false,
            JsonValueKind.False => false,
            JsonValueKind.Null => false,
            _ => false,
        };
    }
}