using System.Net;
using System.Net.Http.Json;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.Application.Auth;
using AcademicTopicSelectionService.Application.Dictionaries.UserRoles;
using AcademicTopicSelectionService.Infrastructure.Data;
using AcademicTopicSelectionService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AcademicTopicSelectionService.IntegrationTests.Controllers;

[Collection(DatabaseCollection.CollectionName)]
public sealed class AuthControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "/api/v1/auth";

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    private Guid _testRoleId;

    public AuthControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        await _fixture.ResetRedisAsync();

        // Создаём тестовую роль — нужна для регистрации пользователей (только администратор)
        var adminClient = _fixture.CreateAuthenticatedClient(AppRoles.Admin);
        var response = await adminClient.PostAsJsonAsync("/api/v1/user-roles",
            new { CodeName = "Student", DisplayName = "Студент" });
        response.EnsureSuccessStatusCode();
        var role = await response.Content.ReadFromJsonAsync<UserRoleDto>();
        _testRoleId = role!.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // POST /api/v1/auth/register
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Register_Returns201_WhenDataIsValid()
    {
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/register", new
        {
            Email = "ivan@test.com",
            Password = "password123",
            FirstName = "Ivan",
            LastName = "Ivanov",
            RoleId = _testRoleId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.Email.Should().Be("ivan@test.com");
        body.Role.Should().Be("Student");
    }

    [Fact]
    public async Task Register_Returns409_WhenEmailAlreadyExists()
    {
        await RegisterUserAsync("duplicate@test.com");

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/register", new
        {
            Email = "duplicate@test.com",
            Password = "password123",
            FirstName = "Ivan",
            LastName = "Ivanov",
            RoleId = _testRoleId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("notanemail", "password123", "Ivan", "Ivanov")]
    [InlineData("test@test.com", "123", "Ivan", "Ivanov")]
    [InlineData("test@test.com", "password123", "", "Ivanov")]
    [InlineData("test@test.com", "password123", "Ivan", "")]
    public async Task Register_Returns400_WhenValidationFails(
        string email, string password, string firstName, string lastName)
    {
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/register", new
        {
            Email = email,
            Password = password,
            FirstName = firstName,
            LastName = lastName,
            RoleId = _testRoleId
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_Returns400_WhenRoleIdIsEmpty()
    {
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/register", new
        {
            Email = "roleid-empty@test.com",
            Password = "password123",
            FirstName = "Ivan",
            LastName = "Ivanov",
            RoleId = Guid.Empty
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_NormalizesEmail_ToLowercase()
    {
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/register", new
        {
            Email = "Ivan@TEST.COM",
            Password = "password123",
            FirstName = "Ivan",
            LastName = "Ivanov",
            RoleId = _testRoleId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.Email.Should().Be("ivan@test.com");
    }

    // -------------------------------------------------------------------------
    // POST /api/v1/auth/login
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Login_Returns200_WhenCredentialsAreValid()
    {
        await RegisterUserAsync("user@test.com", "mypassword");

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/login", new
        {
            Email = "user@test.com",
            Password = "mypassword"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.Email.Should().Be("user@test.com");
    }

    [Fact]
    public async Task Login_Returns401_WhenEmailNotFound()
    {
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/login", new
        {
            Email = "nobody@test.com",
            Password = "password123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_Returns401_WhenPasswordIsWrong()
    {
        await RegisterUserAsync("user@test.com", "correctpassword");

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/login", new
        {
            Email = "user@test.com",
            Password = "wrongpassword"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_IsCaseInsensitive_ForEmail()
    {
        await RegisterUserAsync("user@test.com");

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/login", new
        {
            Email = "USER@TEST.COM",
            Password = "password123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_Returns401_WhenUserIsDeactivated()
    {
        var auth = await RegisterUserAsync("inactive@test.com");
        await SetUserIsActiveAsync(auth.UserId, isActive: false);

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/login", new
        {
            Email = "inactive@test.com",
            Password = "password123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("", "password123")]
    [InlineData("user@test.com", "")]
    public async Task Login_Returns400_WhenEmailOrPasswordIsEmpty(string email, string password)
    {
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/login", new
        {
            Email = email,
            Password = password
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // POST /api/v1/auth/refresh
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Refresh_Returns200_WhenTokenIsValid()
    {
        var auth = await RegisterUserAsync("user@test.com");

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/refresh", new
        {
            RefreshToken = auth.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Refresh_Returns401_WhenTokenIsInvalid()
    {
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/refresh", new
        {
            RefreshToken = "this-is-not-a-valid-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Returns401_WhenUserIsDeactivated()
    {
        var auth = await RegisterUserAsync("inactive-refresh@test.com");
        await SetUserIsActiveAsync(auth.UserId, isActive: false);

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/refresh", new
        {
            RefreshToken = auth.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Returns401_WhenUserNoLongerExistsInDatabase()
    {
        var auth = await RegisterUserAsync("deleted@test.com");
        await RemoveUserAsync(auth.UserId);

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/refresh", new
        {
            RefreshToken = auth.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_RotatesToken_SoOldTokenCannotBeReused()
    {
        var auth = await RegisterUserAsync("user@test.com");
        var oldRefreshToken = auth.RefreshToken;

        // Первый refresh — успешный, токен ротируется
        var refreshResponse = await _client.PostAsJsonAsync($"{BaseUrl}/refresh", new
        {
            RefreshToken = oldRefreshToken
        });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Повторное использование старого токена должно вернуть 401
        var retryResponse = await _client.PostAsJsonAsync($"{BaseUrl}/refresh", new
        {
            RefreshToken = oldRefreshToken
        });
        retryResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ReturnsNewUniqueTokens_EachTime()
    {
        var auth = await RegisterUserAsync("user@test.com");

        var first = await _client.PostAsJsonAsync($"{BaseUrl}/refresh", new
        {
            RefreshToken = auth.RefreshToken
        });
        var firstBody = await first.Content.ReadFromJsonAsync<AuthResponse>();

        var second = await _client.PostAsJsonAsync($"{BaseUrl}/refresh", new
        {
            RefreshToken = firstBody!.RefreshToken
        });
        var secondBody = await second.Content.ReadFromJsonAsync<AuthResponse>();

        firstBody.AccessToken.Should().NotBe(secondBody!.AccessToken);
        firstBody.RefreshToken.Should().NotBe(secondBody.RefreshToken);
    }

    // -------------------------------------------------------------------------
    // POST /api/v1/auth/logout
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Logout_Returns204_WhenTokenIsValid()
    {
        var auth = await RegisterUserAsync("user@test.com");

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/logout", new
        {
            RefreshToken = auth.RefreshToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_Returns401_WhenTokenIsInvalid()
    {
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/logout", new
        {
            RefreshToken = "non-existent-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_Returns400_WhenRefreshTokenIsEmpty()
    {
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/logout", new
        {
            RefreshToken = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Logout_PreventsRefresh_AfterLogout()
    {
        var auth = await RegisterUserAsync("user@test.com");

        var logoutResponse = await _client.PostAsJsonAsync($"{BaseUrl}/logout", new
        {
            RefreshToken = auth.RefreshToken
        });
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refreshResponse = await _client.PostAsJsonAsync($"{BaseUrl}/refresh", new
        {
            RefreshToken = auth.RefreshToken
        });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Вспомогательные методы
    // -------------------------------------------------------------------------

    private async Task SetUserIsActiveAsync(Guid userId, bool isActive)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FindAsync(userId);
        user!.IsActive = isActive;
        await db.SaveChangesAsync();
    }

    private async Task RemoveUserAsync(Guid userId)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FindAsync(userId);
        db.Users.Remove(user!);
        await db.SaveChangesAsync();
    }

    private async Task<AuthResponse> RegisterUserAsync(
        string email = "user@test.com",
        string password = "password123")
    {
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/register", new
        {
            Email = email,
            Password = password,
            FirstName = "Ivan",
            LastName = "Ivanov",
            RoleId = _testRoleId
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }
}
