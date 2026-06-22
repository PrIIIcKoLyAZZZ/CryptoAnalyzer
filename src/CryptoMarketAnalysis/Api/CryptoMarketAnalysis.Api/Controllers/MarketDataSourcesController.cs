using CryptoMarketAnalysis.Application.Contracts.MarketDataSources;
using Microsoft.AspNetCore.Mvc;

namespace CryptoMarketAnalysis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class MarketDataSourcesController : ControllerBase
{
    private readonly IGetMarketDataSourcesUseCase _getMarketDataSourcesUseCase;

    public MarketDataSourcesController(IGetMarketDataSourcesUseCase getMarketDataSourcesUseCase)
    {
        _getMarketDataSourcesUseCase = getMarketDataSourcesUseCase
            ?? throw new ArgumentNullException(nameof(getMarketDataSourcesUseCase));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<MarketDataSourceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<MarketDataSourceDto>>> GetAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<MarketDataSourceDto> dataSources =
            await _getMarketDataSourcesUseCase.ExecuteAsync(cancellationToken);
        return Ok(dataSources);
    }
}