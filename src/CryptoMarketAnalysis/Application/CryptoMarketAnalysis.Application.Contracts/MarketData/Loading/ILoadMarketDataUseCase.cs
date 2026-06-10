namespace CryptoMarketAnalysis.Application.Contracts.MarketData.Loading;

public interface ILoadMarketDataUseCase
{
    Task<LoadMarketDataResponse> ExecuteAsync(
        LoadMarketDataRequest request,
        CancellationToken cancellationToken = default);
}