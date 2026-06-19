using System.ComponentModel.DataAnnotations;

namespace Oryxen.Application.Auth.Contracts;

public sealed record RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = null!;

    [Required]
    [MinLength(8)]
    public string Password { get; init; } = null!;

    [Required]
    [MaxLength(120)]
    public string FullName { get; init; } = null!;
}
