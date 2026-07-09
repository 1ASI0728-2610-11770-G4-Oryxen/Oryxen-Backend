using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oryxen.Application.Plants;
using Oryxen.Application.Plants.Contracts;
using Oryxen.Application.Weather.Contracts;
using Oryxen.Domain.Constants;

namespace Oryxen.API.Controllers;

/// <summary>
/// Plant Management bounded context: CRUD over a farmer's plants, Sensor Lite
/// assignment, watering logs and the OpenWeatherMap ambient-weather complement.
/// </summary>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
[Authorize(Roles = $"{Roles.Farmer},{Roles.Admin}")]
public sealed class PlantsController : ControllerBase
{
    private readonly IPlantService _plants;

    public PlantsController(IPlantService plants) => _plants = plants;

    /// <summary>Lists the plants owned by a user. Farmers may only query themselves; admins, anyone.</summary>
    [HttpGet("users/{userId:guid}/plants")]
    [ProducesResponseType(typeof(IReadOnlyList<PlantResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<PlantResponse>>> GetByUser(Guid userId, CancellationToken cancellationToken)
    {
        if (!IsSelfOrAdmin(userId))
        {
            return Forbid();
        }

        return Ok(await _plants.GetByUserAsync(userId, cancellationToken));
    }

    /// <summary>Returns a single plant with its recent telemetry metrics and watering logs.</summary>
    [HttpGet("plants/{id:guid}")]
    [ProducesResponseType(typeof(PlantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlantResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _plants.GetByIdAsync(id, CurrentUserId, IsAdmin, cancellationToken));

    /// <summary>Creates a plant owned by the authenticated user.</summary>
    [HttpPost("plants")]
    [ProducesResponseType(typeof(PlantResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePlantRequest request, CancellationToken cancellationToken)
    {
        var response = await _plants.CreateAsync(CurrentUserId, request, cancellationToken);
        return Created($"/api/v1/plants/{response.Id}", response);
    }

    /// <summary>Updates the editable fields of a plant.</summary>
    [HttpPut("plants/{id:guid}")]
    [ProducesResponseType(typeof(PlantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlantResponse>> Update(Guid id, [FromBody] UpdatePlantRequest request, CancellationToken cancellationToken) =>
        Ok(await _plants.UpdateAsync(id, CurrentUserId, IsAdmin, request, cancellationToken));

    /// <summary>Deletes a plant and its watering logs.</summary>
    [HttpDelete("plants/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _plants.DeleteAsync(id, CurrentUserId, IsAdmin, cancellationToken);
        return NoContent();
    }

    /// <summary>Records a watering event and recomputes the next watering date.</summary>
    [HttpPost("plants/{id:guid}/watering")]
    [ProducesResponseType(typeof(PlantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlantResponse>> Water(Guid id, [FromBody] WaterPlantRequest request, CancellationToken cancellationToken) =>
        Ok(await _plants.WaterAsync(id, CurrentUserId, IsAdmin, request, cancellationToken));

    /// <summary>Binds a Sensor Lite device to the plant (sensor assignment).</summary>
    [HttpPost("plants/{id:guid}/sensor")]
    [ProducesResponseType(typeof(PlantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlantResponse>> AssignSensor(Guid id, [FromBody] AssignSensorRequest request, CancellationToken cancellationToken) =>
        Ok(await _plants.AssignSensorAsync(id, CurrentUserId, IsAdmin, request, cancellationToken));

    /// <summary>Ambient weather for the plant's farm location, complementing Sensor Lite telemetry.</summary>
    [HttpGet("plants/{id:guid}/weather")]
    [ProducesResponseType(typeof(WeatherResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<WeatherResponse>> GetWeather(Guid id, CancellationToken cancellationToken) =>
        Ok(await _plants.GetWeatherAsync(id, CurrentUserId, IsAdmin, cancellationToken));

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin => User.IsInRole(Roles.Admin);

    private bool IsSelfOrAdmin(Guid userId) =>
        IsAdmin || CurrentUserId == userId;
}
