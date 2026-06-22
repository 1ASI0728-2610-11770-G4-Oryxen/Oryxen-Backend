using System.ComponentModel.DataAnnotations;

namespace Oryxen.Application.Plants.Contracts;

/// <summary>Payload to register a new plant for the authenticated farmer.</summary>
public sealed record CreatePlantRequest
{
    [Required]
    [MaxLength(120)]
    public string Name { get; init; } = null!;

    [Required]
    [MaxLength(80)]
    public string Type { get; init; } = null!;

    [MaxLength(512)]
    public string? ImgUrl { get; init; }

    [MaxLength(2000)]
    public string? Bio { get; init; }

    [MaxLength(200)]
    public string? Location { get; init; }
}
