using System.ComponentModel.DataAnnotations;

namespace Oryxen.Application.Auth.Contracts;

public sealed record RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = null!;
}
