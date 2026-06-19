using Oryxen.Application.Auth.Contracts;
using Oryxen.Application.Common.Exceptions;
using Oryxen.Application.Common.Interfaces;
using Oryxen.Application.Common.Models;
using Oryxen.Domain.Constants;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Enums;
using Oryxen.Domain.Repositories;

namespace Oryxen.Application.Auth;

/// <summary>
/// Application service orchestrating the Sprint 1 authentication flows: local
/// registration, credential login and refresh-token rotation. Issues JWT access
/// tokens carrying the user's RBAC role claims.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly IUserAccountRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IUserAccountRepository users,
        IRoleRepository roles,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _roles = roles;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = Normalize(request.Email);

        if (await _users.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new EmailAlreadyExistsException(email);
        }

        var farmerRole = await _roles.GetByNameAsync(Roles.Farmer, cancellationToken)
            ?? throw new InvalidOperationException($"The '{Roles.Farmer}' role has not been seeded.");

        var user = new UserAccount
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FullName = request.FullName.Trim(),
            Status = AccountStatus.Active
        };
        user.Roles.Add(farmerRole);
        user.Subscription = new Subscription
        {
            Plan = SubscriptionPlan.Freemium,
            Status = SubscriptionStatus.Active,
            StartedAt = DateTime.UtcNow
        };

        var access = ApplyTokens(user, out var refreshToken);

        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(user, access, refreshToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = Normalize(request.Email);

        var user = await _users.GetByEmailAsync(email, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        if (!user.IsActive)
        {
            throw new InvalidCredentialsException();
        }

        var access = ApplyTokens(user, out var refreshToken);

        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(user, access, refreshToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenGenerator.HashRefreshToken(request.RefreshToken);

        var user = await _users.GetByRefreshTokenHashAsync(tokenHash, cancellationToken);
        if (user is null || !user.IsRefreshTokenValid(tokenHash))
        {
            throw new InvalidRefreshTokenException();
        }

        var access = ApplyTokens(user, out var refreshToken);

        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(user, access, refreshToken);
    }

    /// <summary>Generates a new access/refresh token pair and stores the hashed refresh token on the user.</summary>
    private TokenResult ApplyTokens(UserAccount user, out string refreshToken)
    {
        var roleNames = user.Roles.Select(r => r.Name).ToArray();
        var access = _tokenGenerator.GenerateAccessToken(user, roleNames);
        var refresh = _tokenGenerator.GenerateRefreshToken();

        refreshToken = refresh.Token;
        user.SetRefreshToken(_tokenGenerator.HashRefreshToken(refresh.Token), refresh.ExpiresAt);

        return access;
    }

    private static AuthResponse ToResponse(UserAccount user, TokenResult access, string refreshToken) =>
        new(
            access.Token,
            refreshToken,
            access.ExpiresAt,
            user.Email,
            user.FullName,
            user.Roles.Select(r => r.Name).ToArray());

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
