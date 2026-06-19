using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oryxen.Application.Telemetry;
using Oryxen.Application.Telemetry.Contracts;
using Oryxen.Domain.Constants;

namespace Oryxen.API.Controllers;

/// <summary>
/// IoT telemetry endpoints for the Sensor Lite device: ingestion of new readings and
/// retrieval of a plant's recent history.
/// </summary>
[ApiController]
[Route("api/v1/telemetry")]
[Produces("application/json")]
public sealed class TelemetryController : ControllerBase
{
    private readonly ITelemetryService _telemetryService;

    public TelemetryController(ITelemetryService telemetryService) => _telemetryService = telemetryService;

    /// <summary>
    /// Ingests a telemetry reading from a Sensor Lite device and returns it with its
    /// derived Health Score. Open during Sprint 1 for the local IoT simulator; production
    /// will authenticate devices with per-device API keys.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TelemetryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Ingest([FromBody] TelemetryIngestRequest request, CancellationToken cancellationToken)
    {
        var response = await _telemetryService.IngestAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Returns the most recent telemetry readings for a plant within an optional time window.</summary>
    [HttpGet("{plantId:guid}")]
    [Authorize(Roles = $"{Roles.Farmer},{Roles.Admin}")]
    [ProducesResponseType(typeof(IReadOnlyList<TelemetryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<TelemetryResponse>>> GetByPlant(
        Guid plantId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        return Ok(await _telemetryService.GetByPlantAsync(plantId, from, to, cancellationToken));
    }
}
