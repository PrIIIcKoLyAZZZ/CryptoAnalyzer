using CryptoMarketAnalysis.Application.Contracts.MarketData.External;

namespace CryptoMarketAnalysis.Application.Abstractions.MarketData;

public interface IMarketDataProvider
{
    string SourceCode { get; }

    Task<IReadOnlyCollection<ExternalMarketDataPointDto>> GetHistoricalAsync(
        string symbol,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    Task<ExternalMarketDataPointDto?> GetLatestAsync(
        string symbol,
        CancellationToken cancellationToken = default);
}