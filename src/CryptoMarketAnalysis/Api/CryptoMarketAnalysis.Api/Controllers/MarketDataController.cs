using CryptoMarketAnalysis.Application.Contracts.MarketData;
using CryptoMarketAnalysis.Application.Contracts.MarketData.Historical;
using CryptoMarketAnalysis.Application.Contracts.MarketData.Loading;
using Microsoft.AspNetCore.Mvc;

namespace CryptoMarketAnalysis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class MarketDataController : ControllerBase
{
    private readonly IGetHistoricalMarketDataUseCase _getHistoricalMarketDataUseCase;
    private readonly ILoadMarketDataUseCase _loadMarketDataUseCase;

    public MarketDataController(IGetHistoricalMarketDataUseCase getHistoricalMarketDataUseCase, ILoadMarketDataUseCase loadMarketDataUseCase)
    {
        _getHistoricalMarketDataUseCase = getHistoricalMarketDataUseCase
            ?? throw new ArgumentNullException(nameof(getHistoricalMarketDataUseCase));
        _loadMarketDataUseCase = loadMarketDataUseCase
            ?? throw new ArgumentNullException(nameof(loadMarketDataUseCase));
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(HistoricalMarketDataResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<HistoricalMarketDataResponse>> GetHistoryAsync(
        [FromQuery] string symbol,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] string? marketDataSourceCode,
        CancellationToken cancellationToken)
    {
        var request = new HistoricalMarketDataRequest(
            symbol,
            fromUtc,
            toUtc,
            marketDataSourceCode);

        HistoricalMarketDataResponse result =
            await _getHistoricalMarketDataUseCase.ExecuteAsync(request, cancellationToken);

        return Ok(result);
    }

    [HttpPost("load")]
    [ProducesResponseType(typeof(LoadMarketDataResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoadMarketDataResponse>> LoadAsync(
        [FromBody] LoadMarketDataRequest request,
        CancellationToken cancellationToken)
    {
        LoadMarketDataResponse result =
            await _loadMarketDataUseCase.ExecuteAsync(request, cancellationToken);
        return Ok(result);
    }
}