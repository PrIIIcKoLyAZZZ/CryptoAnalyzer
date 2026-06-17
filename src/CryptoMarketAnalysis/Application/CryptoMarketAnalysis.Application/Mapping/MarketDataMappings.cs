using CryptoMarketAnalysis.Application.Contracts.MarketData;
using CryptoMarketAnalysis.Domain.Entities;

namespace CryptoMarketAnalysis.Application.Mapping;

public static class MarketDataMappings
{
    public static MarketDataPointDto ToDto(this MarketDataPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);

        return new MarketDataPointDto(
            AssetId: point.AssetId,
            MarketDataSourceId: point.MarketDataSourceId,
            TimestampUtc: point.TimestampUtc,
            PriceUsd: point.PriceUsd,
            MarketCapUsd: point.MarketCapUsd,
            Volume24hUsd: point.Volume24hUsd);
    }
}