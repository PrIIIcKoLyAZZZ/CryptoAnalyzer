using CryptoMarketAnalysis.Application.Contracts.MarketDataSources;
using CryptoMarketAnalysis.Domain.Entities;

namespace CryptoMarketAnalysis.Application.Mapping;

public static class MarketDataSourceMappings
{
    public static MarketDataSourceDto ToDto(this MarketDataSource marketDataSource)
    {
        ArgumentNullException.ThrowIfNull(marketDataSource);

        return new MarketDataSourceDto(
            Id: marketDataSource.Id,
            Code: marketDataSource.Code.Value,
            Name: marketDataSource.Name,
            IsActive: marketDataSource.IsActive);
    }
}