using System.ComponentModel.DataAnnotations;

namespace Oryxen.Application.Plants.Contracts;

/// <summary>Editable fields of an existing plant.</summary>
public sealed record UpdatePlantRequest
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

    /// <summary>Optional status override ("healthy" / "warning" / "critical"); case-insensitive.</summary>
    [MaxLength(20)]
    public string? Status { get; init; }
}
