using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oryxen.Application.AI;
using Oryxen.Application.AI.Contracts;
using Oryxen.Domain.Constants;

namespace Oryxen.API.Controllers;

/// <summary>
/// Artificial Intelligence bounded context: multimodal plant health diagnosis.
/// Uploads a plant photograph together with the plant's latest Sensor Lite telemetry
/// and returns the Gemini Vision analysis for general plant anomalies, diseases and deficiencies.
/// </summary>
[ApiController]
[Route("api/v1/ai")]
[Produces("application/json")]
[Authorize(Roles = $"{Roles.Farmer},{Roles.Admin}")]
public sealed class AiController : ControllerBase
{
    private readonly IDiagnosisService _diagnoses;
    private readonly IChatService _chat;
    private readonly IWebHostEnvironment _env;

    public AiController(IDiagnosisService diagnoses, IChatService chat, IWebHostEnvironment env)
    {
        _diagnoses = diagnoses;
        _chat = chat;
        _env = env;
    }

    /// <summary>
    /// Creates a new multimodal diagnosis: uploads a crop photo (multipart) for the given
    /// plant, enriches the AI prompt with the latest telemetry and persists the result.
    /// </summary>
    [HttpPost("diagnoses")]
    [ProducesResponseType(typeof(DiagnosisResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Create(
        [FromForm] Guid plantId,
        IFormFile image,
        CancellationToken ct)
    {
        if (image.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "An image file is required.",
                Type = "https://httpstatuses.io/400",
                Instance = HttpContext.Request.Path
            });
        }

        await using var stream = image.OpenReadStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        var response = await _diagnoses.CreateAsync(CurrentUserId, plantId, bytes, image.ContentType, ct);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>Returns a single diagnosis by id.</summary>
    [HttpGet("diagnoses/{id:guid}")]
    [ProducesResponseType(typeof(DiagnosisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiagnosisResponse>> GetById(Guid id, CancellationToken ct) =>
        Ok(await _diagnoses.GetByIdAsync(id, ct));

    /// <summary>Returns the diagnosis history for a plant, newest first.</summary>
    [HttpGet("plants/{plantId:guid}/diagnoses")]
    [ProducesResponseType(typeof(IReadOnlyList<DiagnosisResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DiagnosisResponse>>> GetByPlant(Guid plantId, CancellationToken ct) =>
        Ok(await _diagnoses.GetByPlantAsync(plantId, ct));

    /// <summary>
    /// Conversational plant-care assistant. The reply is generated server-side (Gemini),
    /// so no AI API key ever reaches the web or mobile clients. In Development without a
    /// configured key it degrades to an explicitly-labeled fallback reply instead of 502,
    /// keeping local demos usable.
    /// </summary>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _chat.ChatAsync(request, ct));
        }
        catch (Application.Common.Exceptions.ExternalServiceException) when (_env.IsDevelopment())
        {
            return Ok(new ChatResponse(
                "(Development fallback — configure GeminiVision__ApiKey for real AI replies.) " +
                "General tip: check soil moisture before watering; most houseplants prefer the top " +
                "2-3 cm of soil to dry out between waterings, and steady indirect light.",
                "fallback-dev",
                DateTime.UtcNow));
        }
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
