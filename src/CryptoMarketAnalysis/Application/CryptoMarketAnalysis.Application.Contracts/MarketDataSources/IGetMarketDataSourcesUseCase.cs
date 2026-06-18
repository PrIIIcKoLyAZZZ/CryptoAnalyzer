namespace CryptoMarketAnalysis.Application.Contracts.MarketDataSources;

public interface IGetMarketDataSourcesUseCase
{
    Task<IReadOnlyCollection<MarketDataSourceDto>> ExecuteAsync(
        CancellationToken cancellationToken = default);
}