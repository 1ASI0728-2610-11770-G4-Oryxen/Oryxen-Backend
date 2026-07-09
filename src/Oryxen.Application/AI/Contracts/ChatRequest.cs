using System.ComponentModel.DataAnnotations;

namespace Oryxen.Application.AI.Contracts;

/// <summary>A user message for the Oryxen plant-care assistant.</summary>
public sealed record ChatRequest
{
    /// <summary>The user's question or message.</summary>
    [Required]
    [MaxLength(2000)]
    public string Message { get; init; } = null!;

    /// <summary>
    /// Optional context the client wants the assistant to consider (e.g. the plant's
    /// name, type and latest telemetry, or a compact transcript of the conversation).
    /// </summary>
    [MaxLength(6000)]
    public string? Context { get; init; }
}
