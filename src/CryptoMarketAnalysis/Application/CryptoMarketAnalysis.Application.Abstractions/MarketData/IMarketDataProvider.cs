using CryptoMarketAnalysis.Application.Contracts.MarketData;

namespace CryptoMarketAnalysis.Application.Abstractions.MarketData;

public interface IMarketDataProvider
{
    Task<IReadOnlyCollection<MarketDataPointDto>> GetHistoricalAsync(
        string symbol,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    Task<MarketDataPointDto?> GetLatestAsync(
        string symbol,
        CancellationToken cancellationToken = default);
}