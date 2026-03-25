using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Auth;
using AcademicTopicSelectionService.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.Auth;

public sealed class AuthServiceTests
{
    private readonly IUsersRepository _usersRepo = Substitute.For<IUsersRepository>();
    private readonly IRefreshTokenCache _tokenCache = Substitute.For<IRefreshTokenCache>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwtGenerator = Substitute.For<IJwtTokenGenerator>();

    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_usersRepo, _tokenCache, _passwordHasher, _jwtGenerator);

        _jwtGenerator.GenerateAccessToken(Arg.Any<User>()).Returns("access-token");
        _jwtGenerator.GenerateRefreshTokenValue().Returns("refresh-token");
        _jwtGenerator.GetRefreshTokenExpiration().Returns(DateTime.UtcNow.AddDays(30));
    }

    // -------------------------------------------------------------------------
    // LoginAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LoginAsync_ReturnsInvalidCredentials_WhenUserNotFound()
    {
        _usersRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _sut.LoginAsync(new LoginRequest("test@test.com", "pass"), CancellationToken.None);

        result.Error.Should().Be(AuthError.InvalidCredentials);
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_ReturnsUserInactive_WhenUserIsDeactivated()
    {
        _usersRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(MakeUser(isActive: false));

        var result = await _sut.LoginAsync(new LoginRequest("test@test.com", "pass"), CancellationToken.None);

        result.Error.Should().Be(AuthError.UserInactive);
    }

    [Fact]
    public async Task LoginAsync_ReturnsInvalidCredentials_WhenPasswordIsWrong()
    {
        _usersRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(MakeUser());
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await _sut.LoginAsync(new LoginRequest("test@test.com", "wrong"), CancellationToken.None);

        result.Error.Should().Be(AuthError.InvalidCredentials);
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokens_WhenCredentialsAreValid()
    {
        var user = MakeUser(email: "test@test.com");
        _usersRepo.GetByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("pass", user.PasswordHash).Returns(true);

        var result = await _sut.LoginAsync(new LoginRequest("test@test.com", "pass"), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value!.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.Email.Should().Be(user.Email);
        result.Value.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task LoginAsync_StoresRefreshToken_InCache()
    {
        var user = MakeUser();
        _usersRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        await _sut.LoginAsync(new LoginRequest("test@test.com", "pass"), CancellationToken.None);

        await _tokenCache.Received(1).StoreAsync(
            "refresh-token", user.Id, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("  TEST@EXAMPLE.COM  ", "test@example.com")]
    [InlineData("User@Domain.Org", "user@domain.org")]
    public async Task LoginAsync_NormalizesEmail_BeforeLookup(string inputEmail, string normalizedEmail)
    {
        _usersRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await _sut.LoginAsync(new LoginRequest(inputEmail, "pass"), CancellationToken.None);

        await _usersRepo.Received(1).GetByEmailAsync(normalizedEmail, Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // RegisterAsync — валидация входных данных
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("notanemail")]
    [InlineData("   ")]
    public async Task RegisterAsync_ReturnsValidation_WhenEmailIsInvalid(string email)
    {
        var result = await _sut.RegisterAsync(
            new RegisterRequest(email, "password123", "Ivan", "Ivanov", null, Guid.NewGuid()),
            CancellationToken.None);

        result.Error.Should().Be(AuthError.Validation);
        await _usersRepo.DidNotReceive().CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("1")]
    [InlineData("12345")]
    [InlineData("")]
    public async Task RegisterAsync_ReturnsValidation_WhenPasswordIsTooShort(string password)
    {
        var result = await _sut.RegisterAsync(
            new RegisterRequest("test@test.com", password, "Ivan", "Ivanov", null, Guid.NewGuid()),
            CancellationToken.None);

        result.Error.Should().Be(AuthError.Validation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RegisterAsync_ReturnsValidation_WhenFirstNameIsEmpty(string firstName)
    {
        var result = await _sut.RegisterAsync(
            new RegisterRequest("test@test.com", "password123", firstName, "Ivanov", null, Guid.NewGuid()),
            CancellationToken.None);

        result.Error.Should().Be(AuthError.Validation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RegisterAsync_ReturnsValidation_WhenLastNameIsEmpty(string lastName)
    {
        var result = await _sut.RegisterAsync(
            new RegisterRequest("test@test.com", "password123", "Ivan", lastName, null, Guid.NewGuid()),
            CancellationToken.None);

        result.Error.Should().Be(AuthError.Validation);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsValidation_WhenRoleIdIsEmpty()
    {
        var result = await _sut.RegisterAsync(
            new RegisterRequest("test@test.com", "password123", "Ivan", "Ivanov", null, Guid.Empty),
            CancellationToken.None);

        result.Error.Should().Be(AuthError.Validation);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsEmailAlreadyExists_WhenEmailTaken()
    {
        _usersRepo.ExistsByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.RegisterAsync(
            new RegisterRequest("test@test.com", "password123", "Ivan", "Ivanov", null, Guid.NewGuid()),
            CancellationToken.None);

        result.Error.Should().Be(AuthError.EmailAlreadyExists);
        await _usersRepo.DidNotReceive().CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_ChecksDuplicateEmail_UsingNormalizedForm()
    {
        _usersRepo.ExistsByEmailAsync("test@example.com", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.RegisterAsync(
            new RegisterRequest("  TEST@EXAMPLE.COM  ", "password123", "Ivan", "Ivanov", null, Guid.NewGuid()),
            CancellationToken.None);

        result.Error.Should().Be(AuthError.EmailAlreadyExists);
        await _usersRepo.Received(1).ExistsByEmailAsync("test@example.com", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_ReturnsTokens_WhenDataIsValid()
    {
        var createdUser = MakeUser(email: "test@test.com");
        _usersRepo.ExistsByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(false);
        _usersRepo.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(createdUser);

        var result = await _sut.RegisterAsync(
            new RegisterRequest("test@test.com", "password123", "Ivan", "Ivanov", null, Guid.NewGuid()),
            CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value!.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
    }

    [Fact]
    public async Task RegisterAsync_HashesPassword_BeforeSavingUser()
    {
        _passwordHasher.Hash("password123").Returns("hashed-password");
        _usersRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _usersRepo.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(MakeUser());

        await _sut.RegisterAsync(
            new RegisterRequest("test@test.com", "password123", "Ivan", "Ivanov", null, Guid.NewGuid()),
            CancellationToken.None);

        await _usersRepo.Received(1).CreateAsync(
            Arg.Is<User>(u => u.PasswordHash == "hashed-password"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_NormalizesEmail_BeforeCreatingUser()
    {
        _usersRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _usersRepo.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(MakeUser());

        await _sut.RegisterAsync(
            new RegisterRequest("  TEST@EXAMPLE.COM  ", "password123", "Ivan", "Ivanov", null, Guid.NewGuid()),
            CancellationToken.None);

        await _usersRepo.Received(1).CreateAsync(
            Arg.Is<User>(u => u.Email == "test@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterAsync_TrimsFirstAndLastName_BeforeCreatingUser()
    {
        _usersRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _usersRepo.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(MakeUser());

        await _sut.RegisterAsync(
            new RegisterRequest("test@test.com", "password123", "  Ivan  ", "  Ivanov  ", null, Guid.NewGuid()),
            CancellationToken.None);

        await _usersRepo.Received(1).CreateAsync(
            Arg.Is<User>(u => u.FirstName == "Ivan" && u.LastName == "Ivanov"),
            Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // RefreshAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RefreshAsync_ReturnsInvalidToken_WhenTokenNotInCache()
    {
        _tokenCache.GetUserIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var result = await _sut.RefreshAsync(new RefreshTokenRequest("bad-token"), CancellationToken.None);

        result.Error.Should().Be(AuthError.InvalidToken);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsInvalidToken_WhenUserNotFoundInDb()
    {
        _tokenCache.GetUserIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());
        _usersRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _sut.RefreshAsync(new RefreshTokenRequest("some-token"), CancellationToken.None);

        result.Error.Should().Be(AuthError.InvalidToken);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsUserInactive_WhenUserIsDeactivated()
    {
        var userId = Guid.NewGuid();
        _tokenCache.GetUserIdAsync("some-token", Arg.Any<CancellationToken>()).Returns(userId);
        _usersRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(MakeUser(isActive: false));

        var result = await _sut.RefreshAsync(new RefreshTokenRequest("some-token"), CancellationToken.None);

        result.Error.Should().Be(AuthError.UserInactive);
    }

    [Fact]
    public async Task RefreshAsync_RemovesOldToken_WhenSuccessful()
    {
        var userId = Guid.NewGuid();
        _tokenCache.GetUserIdAsync("old-token", Arg.Any<CancellationToken>()).Returns(userId);
        _usersRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(MakeUser());

        await _sut.RefreshAsync(new RefreshTokenRequest("old-token"), CancellationToken.None);

        await _tokenCache.Received(1).RemoveAsync("old-token", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_StoresNewToken_WhenSuccessful()
    {
        var userId = Guid.NewGuid();
        var user = MakeUser();
        _tokenCache.GetUserIdAsync("old-token", Arg.Any<CancellationToken>()).Returns(userId);
        _usersRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        await _sut.RefreshAsync(new RefreshTokenRequest("old-token"), CancellationToken.None);

        await _tokenCache.Received(1).StoreAsync(
            "refresh-token", user.Id, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_ReturnsNewTokens_WhenSuccessful()
    {
        var userId = Guid.NewGuid();
        _tokenCache.GetUserIdAsync("old-token", Arg.Any<CancellationToken>()).Returns(userId);
        _usersRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(MakeUser());

        var result = await _sut.RefreshAsync(new RefreshTokenRequest("old-token"), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value!.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
    }

    // -------------------------------------------------------------------------
    // LogoutAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LogoutAsync_ReturnsInvalidToken_WhenTokenNotFound()
    {
        _tokenCache.GetUserIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var result = await _sut.LogoutAsync(new RefreshTokenRequest("bad-token"), CancellationToken.None);

        result.Error.Should().Be(AuthError.InvalidToken);
    }

    [Fact]
    public async Task LogoutAsync_RemovesToken_WhenValid()
    {
        _tokenCache.GetUserIdAsync("valid-token", Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());

        await _sut.LogoutAsync(new RefreshTokenRequest("valid-token"), CancellationToken.None);

        await _tokenCache.Received(1).RemoveAsync("valid-token", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LogoutAsync_ReturnsTrue_WhenSuccessful()
    {
        _tokenCache.GetUserIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());

        var result = await _sut.LogoutAsync(new RefreshTokenRequest("valid-token"), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // Вспомогательные методы
    // -------------------------------------------------------------------------

    private static User MakeUser(
        string email = "user@test.com",
        bool isActive = true,
        Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Email = email,
        PasswordHash = "hashed-password",
        FirstName = "Ivan",
        LastName = "Ivanov",
        RoleId = Guid.NewGuid(),
        IsActive = isActive,
        Role = new UserRole { Id = Guid.NewGuid(), Name = "Student", DisplayName = "Студент" }
    };
}
