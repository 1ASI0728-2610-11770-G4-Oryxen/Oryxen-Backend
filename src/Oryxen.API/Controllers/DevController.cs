using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Oryxen.Application.Telemetry;
using Oryxen.Application.Telemetry.Contracts;

namespace Oryxen.API.Controllers;

[ApiController]
[Route("api/v1/dev")]
[Produces("application/json")]
public sealed class DevController : ControllerBase
{
    private readonly IIoTSimulationService _simulationService;
    private readonly IWebHostEnvironment _env;

    public DevController(IIoTSimulationService simulationService, IWebHostEnvironment env)
    {
        _simulationService = simulationService;
        _env = env;
    }

    [HttpPost("seed-iot")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SeedResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SeedResultResponse>> SeedIotData(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (!_env.IsDevelopment())
        {
            return Forbid("This endpoint is only available in Development environment.");
        }

        var result = await _simulationService.SeedHistoricalDataAsync(days, cancellationToken);
        return Ok(result);
    }
}
