using CryptoMarketAnalysis.Domain.Common;

namespace CryptoMarketAnalysis.Domain.Entities;

public sealed class MarketDataPoint : Entity
{
    public Guid AssetId { get; private set; }

    public Guid ExchangeId { get; private set; }

    public DateTime TimestampUtc { get; private set; }

    public decimal PriceUsd { get; private set; }

    public decimal? MarketCapUsd { get; private set; }

    public decimal? Volume24hUsd { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public MarketDataPoint(
        Guid id,
        Guid assetId,
        Guid exchangeId,
        DateTime timestampUtc,
        decimal priceUsd,
        decimal? marketCapUsd,
        decimal? volume24hUsd,
        DateTime createdAtUtc) : base(id)
    {
        if (assetId == Guid.Empty)
            throw new ArgumentException("Asset id cannot be empty.", nameof(assetId));

        if (exchangeId == Guid.Empty)
            throw new ArgumentException("Exchange id cannot be empty.", nameof(exchangeId));

        if (timestampUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Timestamp must be in UTC.", nameof(timestampUtc));

        if (priceUsd <= 0)
            throw new ArgumentOutOfRangeException(nameof(priceUsd), "Price must be greater than zero.");

        if (marketCapUsd is < 0)
            throw new ArgumentOutOfRangeException(nameof(marketCapUsd), "Market cap cannot be negative.");

        if (volume24hUsd is < 0)
            throw new ArgumentOutOfRangeException(nameof(volume24hUsd), "Volume cannot be negative.");

        if (createdAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Created date must be in UTC.", nameof(createdAtUtc));

        AssetId = assetId;
        ExchangeId = exchangeId;
        TimestampUtc = timestampUtc;
        PriceUsd = priceUsd;
        MarketCapUsd = marketCapUsd;
        Volume24hUsd = volume24hUsd;
        CreatedAtUtc = createdAtUtc;
    }

    public static MarketDataPoint Create(
        Guid assetId,
        Guid exchangeId,
        DateTime timestampUtc,
        decimal priceUsd,
        decimal? marketCapUsd = null,
        decimal? volume24hUsd = null)
    {
        return new MarketDataPoint(
            Guid.NewGuid(),
            assetId,
            exchangeId,
            timestampUtc,
            priceUsd,
            marketCapUsd,
            volume24hUsd,
            DateTime.UtcNow);
    }
}