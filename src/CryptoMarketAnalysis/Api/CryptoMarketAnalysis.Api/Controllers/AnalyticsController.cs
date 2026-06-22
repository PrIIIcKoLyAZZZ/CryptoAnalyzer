using CryptoMarketAnalysis.Application.Contracts.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace CryptoMarketAnalysis.Api.Controllers;

[ApiController]
[Route("api/analytics")]
[Produces("application/json")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IAnalyzePriceChangeUseCase _analyzePriceChangeUseCase;
    private readonly ICalculateVolatilityUseCase _calculateVolatilityUseCase;
    private readonly ICalculateCorrelationUseCase _calculateCorrelationUseCase;

    public AnalyticsController(
        IAnalyzePriceChangeUseCase analyzePriceChangeUseCase,
        ICalculateVolatilityUseCase calculateVolatilityUseCase,
        ICalculateCorrelationUseCase calculateCorrelationUseCase)
    {
        _analyzePriceChangeUseCase = analyzePriceChangeUseCase
            ?? throw new ArgumentNullException(nameof(analyzePriceChangeUseCase));
        _calculateVolatilityUseCase = calculateVolatilityUseCase
            ?? throw new ArgumentNullException(nameof(calculateVolatilityUseCase));
        _calculateCorrelationUseCase = calculateCorrelationUseCase
            ?? throw new ArgumentNullException(nameof(calculateCorrelationUseCase));
    }

    [HttpGet("price-change")]
    [ProducesResponseType(typeof(PriceChangeAnalysisResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PriceChangeAnalysisResponse>> GetPriceChangeAsync(
        [FromQuery] string symbol,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] string? marketDataSourceCode,
        CancellationToken cancellationToken)
    {
        var request = new PriceChangeAnalysisRequest(
            symbol,
            fromUtc,
            toUtc,
            marketDataSourceCode);

        PriceChangeAnalysisResponse result =
            await _analyzePriceChangeUseCase.ExecuteAsync(request, cancellationToken);

        return Ok(result);
    }

    [HttpGet("volatility")]
    [ProducesResponseType(typeof(VolatilityAnalysisResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<VolatilityAnalysisResponse>> GetVolatilityAsync(
        [FromQuery] string symbol,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] string? marketDataSourceCode,
        CancellationToken cancellationToken)
    {
        var request = new VolatilityAnalysisRequest(
            symbol,
            fromUtc,
            toUtc,
            marketDataSourceCode);

        VolatilityAnalysisResponse result =
            await _calculateVolatilityUseCase.ExecuteAsync(request, cancellationToken);

        return Ok(result);
    }

    [HttpGet("correlation")]
    [ProducesResponseType(typeof(CorrelationAnalysisResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CorrelationAnalysisResponse>> GetCorrelationAsync(
        [FromQuery] string baseSymbol,
        [FromQuery] string quoteSymbol,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] string? marketDataSourceCode,
        CancellationToken cancellationToken)
    {
        var request = new CorrelationAnalysisRequest(
            baseSymbol,
            quoteSymbol,
            fromUtc,
            toUtc,
            marketDataSourceCode);

        CorrelationAnalysisResponse result =
            await _calculateCorrelationUseCase.ExecuteAsync(request, cancellationToken);

        return Ok(result);
    }
}