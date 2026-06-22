using NSubstitute;
using Oryxen.Application.Auth;
using Oryxen.Application.Auth.Contracts;
using Oryxen.Application.Common.Exceptions;
using Oryxen.Application.Common.Interfaces;
using Oryxen.Application.Common.Models;
using Oryxen.Domain.Constants;
using Oryxen.Domain.Entities;
using Oryxen.Domain.Enums;
using Oryxen.Domain.Repositories;
using Xunit;

namespace Oryxen.Application.Tests.Auth;

public class AuthServiceTests
{
    private readonly IUserAccountRepository _users = Substitute.For<IUserAccountRepository>();
    private readonly IRoleRepository _roles = Substitute.For<IRoleRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _tokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_users, _roles, _passwordHasher, _tokenGenerator, _unitOfWork);
    }

    private static Role FarmerRole() => new() { Name = Roles.Farmer };

    private static RegisterRequest ValidRegisterRequest() => new()
    {
        Email = "farmer@oryxen.io",
        Password = "Sembrar2026!",
        FullName = "Abraham Estrada"
    };

    private static LoginRequest ValidLoginRequest() => new()
    {
        Email = "farmer@oryxen.io",
        Password = "Sembrar2026!"
    };

    private void SetupTokenGenerator()
    {
        _tokenGenerator.GenerateAccessToken(Arg.Any<UserAccount>(), Arg.Any<IEnumerable<string>>())
            .Returns(new TokenResult("access-jwt", DateTime.UtcNow.AddHours(1)));
        _tokenGenerator.GenerateRefreshToken()
            .Returns(new TokenResult("refresh-opaque", DateTime.UtcNow.AddDays(7)));
        _tokenGenerator.HashRefreshToken(Arg.Any<string>())
            .Returns("hashed-refresh-token");
    }

    public class RegisterAsync : AuthServiceTests
    {
        [Fact]
        public async Task Creates_User_And_Returns_Tokens_When_Email_Is_New()
        {
            _users.ExistsByEmailAsync("farmer@oryxen.io", Arg.Any<CancellationToken>())
                .Returns(false);
            _roles.GetByNameAsync(Roles.Farmer, Arg.Any<CancellationToken>())
                .Returns(FarmerRole());
            _passwordHasher.Hash("Sembrar2026!").Returns("bcrypt-hash");
            SetupTokenGenerator();

            var response = await _sut.RegisterAsync(ValidRegisterRequest());

            Assert.Equal("access-jwt", response.AccessToken);
            Assert.Equal("refresh-opaque", response.RefreshToken);
            Assert.Equal("farmer@oryxen.io", response.Email);
            Assert.Equal("Abraham Estrada", response.FullName);
            Assert.Contains(Roles.Farmer, response.Roles);

            await _users.Received(1).AddAsync(Arg.Any<UserAccount>(), Arg.Any<CancellationToken>());
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Hashes_Password_With_BCrypt_Hasher()
        {
            _users.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
            _roles.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(FarmerRole());
            _passwordHasher.Hash("Sembrar2026!").Returns("bcrypt-hash");
            SetupTokenGenerator();

            await _sut.RegisterAsync(ValidRegisterRequest());

            _passwordHasher.Received(1).Hash("Sembrar2026!");
        }

        [Fact]
        public async Task Throws_EmailAlreadyExistsException_When_Email_Is_Duplicate()
        {
            _users.ExistsByEmailAsync("farmer@oryxen.io", Arg.Any<CancellationToken>())
                .Returns(true);

            await Assert.ThrowsAsync<EmailAlreadyExistsException>(
                () => _sut.RegisterAsync(ValidRegisterRequest()));

            await _users.DidNotReceive().AddAsync(Arg.Any<UserAccount>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Normalizes_Email_To_Lowercase_Trimmed()
        {
            _users.ExistsByEmailAsync("farmer@oryxen.io", Arg.Any<CancellationToken>()).Returns(false);
            _roles.GetByNameAsync(Roles.Farmer, Arg.Any<CancellationToken>()).Returns(FarmerRole());
            _passwordHasher.Hash(Arg.Any<string>()).Returns("hash");
            SetupTokenGenerator();

            var request = new RegisterRequest
            {
                Email = "  Farmer@Oryxen.IO  ",
                Password = "Sembrar2026!",
                FullName = "Abraham"
            };

            await _sut.RegisterAsync(request);

            await _users.Received(1).ExistsByEmailAsync("farmer@oryxen.io", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Assigns_Farmer_Role_And_Freemium_Subscription_On_Registration()
        {
            _users.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
            _roles.GetByNameAsync(Roles.Farmer, Arg.Any<CancellationToken>()).Returns(FarmerRole());
            _passwordHasher.Hash(Arg.Any<string>()).Returns("hash");
            SetupTokenGenerator();

            UserAccount? captured = null;
            await _users.AddAsync(Arg.Do<UserAccount>(u => captured = u), Arg.Any<CancellationToken>());

            await _sut.RegisterAsync(ValidRegisterRequest());

            Assert.NotNull(captured);
            Assert.Contains(captured!.Roles, r => r.Name == Roles.Farmer);
            Assert.NotNull(captured.Subscription);
            Assert.Equal(SubscriptionPlan.Freemium, captured.Subscription!.Plan);
            Assert.Equal(SubscriptionStatus.Active, captured.Subscription!.Status);
        }
    }

    public class LoginAsync : AuthServiceTests
    {
        [Fact]
        public async Task Returns_Tokens_When_Credentials_Are_Valid()
        {
            var user = new UserAccount
            {
                Id = Guid.NewGuid(),
                Email = "farmer@oryxen.io",
                PasswordHash = "bcrypt-hash",
                FullName = "Abraham Estrada",
                Status = AccountStatus.Active
            };
            user.Roles.Add(FarmerRole());

            _users.GetByEmailAsync("farmer@oryxen.io", Arg.Any<CancellationToken>()).Returns(user);
            _passwordHasher.Verify("Sembrar2026!", "bcrypt-hash").Returns(true);
            SetupTokenGenerator();

            var response = await _sut.LoginAsync(ValidLoginRequest());

            Assert.Equal("access-jwt", response.AccessToken);
            Assert.Equal("farmer@oryxen.io", response.Email);
            Assert.Contains(Roles.Farmer, response.Roles);
        }

        [Fact]
        public async Task Throws_InvalidCredentialsException_When_Password_Is_Wrong()
        {
            var user = new UserAccount
            {
                Email = "farmer@oryxen.io",
                PasswordHash = "bcrypt-hash",
                Status = AccountStatus.Active
            };

            _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
            _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => _sut.LoginAsync(ValidLoginRequest()));
        }

        [Fact]
        public async Task Throws_InvalidCredentialsException_When_User_Does_Not_Exist()
        {
            _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns((UserAccount?)null);

            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => _sut.LoginAsync(ValidLoginRequest()));

            _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public async Task Throws_InvalidCredentialsException_When_Account_Is_Inactive()
        {
            var user = new UserAccount
            {
                Email = "farmer@oryxen.io",
                PasswordHash = "bcrypt-hash",
                Status = AccountStatus.Suspended
            };

            _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
            _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => _sut.LoginAsync(ValidLoginRequest()));
        }

        [Fact]
        public async Task Normalizes_Email_To_Lowercase_Trimmed_Before_Lookup()
        {
            _users.GetByEmailAsync("farmer@oryxen.io", Arg.Any<CancellationToken>())
                .Returns((UserAccount?)null);

            var request = new LoginRequest
            {
                Email = "  Farmer@Oryxen.IO  ",
                Password = "Sembrar2026!"
            };

            await Assert.ThrowsAsync<InvalidCredentialsException>(() => _sut.LoginAsync(request));

            await _users.Received(1).GetByEmailAsync("farmer@oryxen.io", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Rotates_Refresh_Token_On_Successful_Login()
        {
            var user = new UserAccount
            {
                Email = "farmer@oryxen.io",
                PasswordHash = "bcrypt-hash",
                Status = AccountStatus.Active
            };
            user.Roles.Add(FarmerRole());

            _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
            _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
            SetupTokenGenerator();

            await _sut.LoginAsync(ValidLoginRequest());

            _users.Received(1).Update(user);
            Assert.NotNull(user.RefreshTokenHash);
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
