using CryptoMarketAnalysis.Application.Abstractions.Persistence;
using CryptoMarketAnalysis.Application.Contracts.MarketDataSources;
using CryptoMarketAnalysis.Application.Mapping;
using CryptoMarketAnalysis.Domain.Entities;

namespace CryptoMarketAnalysis.Application.MarketDataSources.GetMarketDataSources;

public class GetMarketDataSourcesUseCase : IGetMarketDataSourcesUseCase
{
    private readonly IMarketDataSourceRepository _marketDataSourceRepository;

    public GetMarketDataSourcesUseCase(IMarketDataSourceRepository marketDataSourceRepository)
    {
        _marketDataSourceRepository = marketDataSourceRepository
            ?? throw new ArgumentNullException(nameof(marketDataSourceRepository));
    }

    public async Task<IReadOnlyCollection<MarketDataSourceDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<MarketDataSource> marketDataSources =
            await _marketDataSourceRepository.GetActiveAsync(cancellationToken);

        return marketDataSources
            .OrderBy(marketDataSource => marketDataSource.Code.Value, StringComparer.Ordinal)
            .Select(marketDataSource => marketDataSource.ToDto())
            .ToList();
    }
}