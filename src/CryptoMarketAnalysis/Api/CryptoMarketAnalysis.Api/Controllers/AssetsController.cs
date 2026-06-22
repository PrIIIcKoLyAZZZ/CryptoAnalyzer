using CryptoMarketAnalysis.Application.Contracts.Assets;
using Microsoft.AspNetCore.Mvc;

namespace CryptoMarketAnalysis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class AssetsController : ControllerBase
{
    private readonly IGetCryptoAssetsUseCase _getCryptoAssetsUseCase;

    public AssetsController(IGetCryptoAssetsUseCase getCryptoAssetsUseCase)
    {
        _getCryptoAssetsUseCase = getCryptoAssetsUseCase
            ?? throw new ArgumentNullException(nameof(getCryptoAssetsUseCase));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<CryptoAssetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<CryptoAssetDto>>> GetAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<CryptoAssetDto> assets =
            await _getCryptoAssetsUseCase.ExecuteAsync(cancellationToken);
        return Ok(assets);
    }
}