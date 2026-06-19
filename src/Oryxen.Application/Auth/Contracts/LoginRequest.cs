using System.ComponentModel.DataAnnotations;

namespace Oryxen.Application.Auth.Contracts;

public sealed record LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = null!;

    [Required]
    public string Password { get; init; } = null!;
}
