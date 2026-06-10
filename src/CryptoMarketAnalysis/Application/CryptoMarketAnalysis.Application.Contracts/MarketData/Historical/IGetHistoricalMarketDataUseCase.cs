namespace CryptoMarketAnalysis.Application.Contracts.MarketData.Historical;

public interface IGetHistoricalMarketDataUseCase
{
    Task<HistoricalMarketDataResponse> ExecuteAsync(
        HistoricalMarketDataRequest request,
        CancellationToken cancellationToken = default);
}