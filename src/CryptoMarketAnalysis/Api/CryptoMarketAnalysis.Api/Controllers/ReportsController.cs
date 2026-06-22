using CryptoMarketAnalysis.Application.Contracts.Reports;
using Microsoft.AspNetCore.Mvc;

namespace CryptoMarketAnalysis.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Produces("application/json")]
public sealed class ReportsController : ControllerBase
{
    private readonly IGenerateMarketAnalysisReportUseCase _useCase;

    public ReportsController(
        IGenerateMarketAnalysisReportUseCase useCase)
    {
        _useCase = useCase;
    }

    [HttpPost("pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [Produces("application/pdf")]
    public async Task<IActionResult> GeneratePdfAsync(
        [FromBody] GenerateMarketAnalysisReportRequest request,
        CancellationToken cancellationToken)
    {
        GenerateMarketAnalysisReportResponse response =
            await _useCase.ExecuteAsync(request, cancellationToken);

        return File(
            response.Content,
            response.ContentType,
            response.FileName);
    }
}