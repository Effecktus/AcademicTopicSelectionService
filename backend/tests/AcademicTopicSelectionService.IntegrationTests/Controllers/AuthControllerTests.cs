using System.Net;
using System.Net.Http.Json;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.Application.Auth;
using AcademicTopicSelectionService.Application.Dictionaries.UserRoles;
using AcademicTopicSelectionService.Infrastructure.Data;
using AcademicTopicSelectionService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AcademicTopicSelectionService.IntegrationTests.Controllers;

[Collection(DatabaseCollection.CollectionName)]
public sealed class AuthControllerTests : IAsyncLifetime
{
    private const string AuthBaseUrl = "/api/v1/auth";
    private const string UsersBaseUrl = "/api/v1/users";
    private const string RefreshCookieName = "refreshToken";

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

        var adminClient = _fixture.CreateAuthenticatedClient(AppRoles.Admin);
        var response = await adminClient.PostAsJsonAsync("/api/v1/user-roles",
            new { CodeName = "Student", DisplayName = "Студент" });
        response.EnsureSuccessStatusCode();
        var role = await response.Content.ReadFromJsonAsync<UserRoleDto>();
        _testRoleId = role!.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // POST /api/v1/users (только администратор)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateUser_Returns201_WhenAdminAndDataIsValid()
    {
        var admin = _fixture.CreateAuthenticatedClient(AppRoles.Admin);
        var response = await admin.PostAsJsonAsync(UsersBaseUrl, new
        {
            Email = "ivan@test.com",
            Password = "password123",
            FirstName = "Ivan",
            LastName = "Ivanov",
            RoleId = _testRoleId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreatedUserDto>();
        body!.Email.Should().Be("ivan@test.com");
        body.Role.Should().Be("Student");
        body.UserId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateUser_Returns409_WhenEmailAlreadyExists()
    {
        await CreateUserViaAdminAsync("duplicate@test.com");

        var admin = _fixture.CreateAuthenticatedClient(AppRoles.Admin);
        var response = await admin.PostAsJsonAsync(UsersBaseUrl, new
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
    [InlineData("test@test.com", "short", "Ivan", "Ivanov")]
    [InlineData("test@test.com", "password123", "", "Ivanov")]
    [InlineData("test@test.com", "password123", "Ivan", "")]
    public async Task CreateUser_Returns400_WhenValidationFails(
        string email, string password, string firstName, string lastName)
    {
        var admin = _fixture.CreateAuthenticatedClient(AppRoles.Admin);
        var response = await admin.PostAsJsonAsync(UsersBaseUrl, new
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
    public async Task CreateUser_Returns400_WhenRoleIdIsEmpty()
    {
        var admin = _fixture.CreateAuthenticatedClient(AppRoles.Admin);
        var response = await admin.PostAsJsonAsync(UsersBaseUrl, new
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
    public async Task CreateUser_Returns400_WhenRoleNotFound()
    {
        var admin = _fixture.CreateAuthenticatedClient(AppRoles.Admin);
        var response = await admin.PostAsJsonAsync(UsersBaseUrl, new
        {
            Email = "norole@test.com",
            Password = "password123",
            FirstName = "Ivan",
            LastName = "Ivanov",
            RoleId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateUser_NormalizesEmail_ToLowercase()
    {
        var admin = _fixture.CreateAuthenticatedClient(AppRoles.Admin);
        var response = await admin.PostAsJsonAsync(UsersBaseUrl, new
        {
            Email = "Ivan@TEST.COM",
            Password = "password123",
            FirstName = "Ivan",
            LastName = "Ivanov",
            RoleId = _testRoleId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreatedUserDto>();
        body!.Email.Should().Be("ivan@test.com");
    }

    [Fact]
    public async Task CreateUser_Returns401_WhenAnonymous()
    {
        var response = await _client.PostAsJsonAsync(UsersBaseUrl, new
        {
            Email = "anon@test.com",
            Password = "password123",
            FirstName = "Ivan",
            LastName = "Ivanov",
            RoleId = _testRoleId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateUser_Returns403_WhenCallerIsNotAdmin()
    {
        var student = _fixture.CreateAuthenticatedClient(AppRoles.Student);
        var response = await student.PostAsJsonAsync(UsersBaseUrl, new
        {
            Email = "student-create@test.com",
            Password = "password123",
            FirstName = "Ivan",
            LastName = "Ivanov",
            RoleId = _testRoleId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Auth_Register_IsRemoved_DoesNotReturn201()
    {
        var response = await _client.PostAsJsonAsync($"{AuthBaseUrl}/register", new
        {
            Email = "x@test.com",
            Password = "password123",
            FirstName = "Ivan",
            LastName = "Ivanov",
            RoleId = _testRoleId
        });

        // Нет действия register: обычно 404; при FallbackPolicy без маршрута иногда 401 до тела ответа.
        response.StatusCode.Should().NotBe(HttpStatusCode.Created);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // POST /api/v1/auth/login
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Login_Returns200_WhenCredentialsAreValid()
    {
        await RegisterUserAsync("user@test.com", "mypassword1");

        var response = await _client.PostAsJsonAsync($"{AuthBaseUrl}/login", new
        {
            Email = "user@test.com",
            Password = "mypassword1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccessTokenDto>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.Email.Should().Be("user@test.com");
        // Refresh-токен не должен быть в теле ответа — он в httpOnly-cookie
        ExtractRefreshTokenFromSetCookie(response).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_Returns401_WhenEmailNotFound()
    {
        var response = await _client.PostAsJsonAsync($"{AuthBaseUrl}/login", new
        {
            Email = "nobody@test.com",
            Password = "password123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_Returns401_WhenPasswordIsWrong()
    {
        await RegisterUserAsync("user@test.com", "correctpassword1");

        var response = await _client.PostAsJsonAsync($"{AuthBaseUrl}/login", new
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

        var response = await _client.PostAsJsonAsync($"{AuthBaseUrl}/login", new
        {
            Email = "USER@TEST.COM",
            Password = "password123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_Returns401_WhenUserIsDeactivated()
    {
        var created = await CreateUserViaAdminAsync("inactive@test.com");
        await SetUserIsActiveAsync(created.UserId, isActive: false);

        var response = await _client.PostAsJsonAsync($"{AuthBaseUrl}/login", new
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
        var response = await _client.PostAsJsonAsync($"{AuthBaseUrl}/login", new
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
        // После логина cookie автоматически сохраняется в _client
        await RegisterUserAsync("user@test.com");

        var response = await _client.PostAsync($"{AuthBaseUrl}/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AccessTokenDto>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        ExtractRefreshTokenFromSetCookie(response).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Refresh_Returns401_WhenNoCookie()
    {
        // _client не логинился — cookie отсутствует
        var response = await _client.PostAsync($"{AuthBaseUrl}/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Returns401_WhenTokenIsInvalid()
    {
        // Отправляем невалидный токен через cookie вручную (клиент без автоматических cookies)
        var rawClient = CreateRawClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{AuthBaseUrl}/refresh");
        request.Headers.Add("Cookie", $"{RefreshCookieName}=this-is-not-a-valid-token");

        var response = await rawClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Returns401_WhenUserIsDeactivated()
    {
        var created = await CreateUserViaAdminAsync("inactive-refresh@test.com");
        await LoginAsAsync("inactive-refresh@test.com", "password123");
        await SetUserIsActiveAsync(created.UserId, isActive: false);

        var response = await _client.PostAsync($"{AuthBaseUrl}/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Returns401_WhenUserNoLongerExistsInDatabase()
    {
        var created = await CreateUserViaAdminAsync("deleted@test.com");
        await LoginAsAsync("deleted@test.com", "password123");
        await RemoveUserAsync(created.UserId);

        var response = await _client.PostAsync($"{AuthBaseUrl}/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_RotatesToken_SoOldTokenCannotBeReused()
    {
        var loginResponse = await LoginAndGetResponseAsync("user@test.com");
        var oldToken = ExtractRefreshTokenFromSetCookie(loginResponse);
        oldToken.Should().NotBeNullOrWhiteSpace();

        // Первый refresh — _client отправляет старый cookie, получает новый
        var firstRefresh = await _client.PostAsync($"{AuthBaseUrl}/refresh", null);
        firstRefresh.StatusCode.Should().Be(HttpStatusCode.OK);

        // Пытаемся использовать старый токен через клиент без cookie-контейнера
        var rawClient = CreateRawClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{AuthBaseUrl}/refresh");
        request.Headers.Add("Cookie", $"{RefreshCookieName}={oldToken}");
        var retryResponse = await rawClient.SendAsync(request);

        retryResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ReturnsNewUniqueTokens_EachTime()
    {
        await RegisterUserAsync("user@test.com");

        var first = await _client.PostAsync($"{AuthBaseUrl}/refresh", null);
        var firstBody = await first.Content.ReadFromJsonAsync<AccessTokenDto>();
        var firstRefreshToken = ExtractRefreshTokenFromSetCookie(first);

        var second = await _client.PostAsync($"{AuthBaseUrl}/refresh", null);
        var secondBody = await second.Content.ReadFromJsonAsync<AccessTokenDto>();
        var secondRefreshToken = ExtractRefreshTokenFromSetCookie(second);

        firstBody!.AccessToken.Should().NotBe(secondBody!.AccessToken);
        firstRefreshToken.Should().NotBe(secondRefreshToken);
    }

    // -------------------------------------------------------------------------
    // POST /api/v1/auth/logout
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Logout_Returns204_WhenTokenIsValid()
    {
        await RegisterUserAsync("user@test.com");

        var response = await _client.PostAsync($"{AuthBaseUrl}/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_Returns204_WhenNoCookiePresent()
    {
        // Идемпотентный выход: нет cookie — уже вышли
        var response = await _client.PostAsync($"{AuthBaseUrl}/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_Returns401_WhenTokenIsInvalid()
    {
        var rawClient = CreateRawClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{AuthBaseUrl}/logout");
        request.Headers.Add("Cookie", $"{RefreshCookieName}=non-existent-token");

        var response = await rawClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_PreventsRefresh_AfterLogout()
    {
        await RegisterUserAsync("user@test.com");

        var logoutResponse = await _client.PostAsync($"{AuthBaseUrl}/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Cookie удалён — refresh возвращает 401
        var refreshResponse = await _client.PostAsync($"{AuthBaseUrl}/refresh", null);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Вспомогательные методы
    // -------------------------------------------------------------------------

    /// <summary>Создаёт HttpClient без автоматического управления cookies (для ручной установки cookie-заголовка).</summary>
    private HttpClient CreateRawClient() =>
        _fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    /// <summary>Извлекает значение refresh-токена из заголовка Set-Cookie ответа.</summary>
    private static string? ExtractRefreshTokenFromSetCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
            return null;

        foreach (var v in values)
        {
            if (v.StartsWith($"{RefreshCookieName}=", StringComparison.OrdinalIgnoreCase))
                return v.Split(';')[0][(RefreshCookieName.Length + 1)..];
        }

        return null;
    }

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

    private async Task<CreatedUserDto> CreateUserViaAdminAsync(
        string email = "user@test.com",
        string password = "password123")
    {
        var admin = _fixture.CreateAuthenticatedClient(AppRoles.Admin);
        var response = await admin.PostAsJsonAsync(UsersBaseUrl, new
        {
            Email = email,
            Password = password,
            FirstName = "Ivan",
            LastName = "Ivanov",
            RoleId = _testRoleId
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedUserDto>())!;
    }

    /// <summary>Логинится и возвращает весь HttpResponseMessage (для извлечения cookie).</summary>
    private async Task<HttpResponseMessage> LoginAndGetResponseAsync(
        string email = "user@test.com",
        string password = "password123")
    {
        await CreateUserViaAdminAsync(email, password);
        var response = await _client.PostAsJsonAsync($"{AuthBaseUrl}/login", new { Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        return response;
    }

    private async Task<AccessTokenDto> LoginAsAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync($"{AuthBaseUrl}/login", new { Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AccessTokenDto>())!;
    }

    private async Task<AccessTokenDto> RegisterUserAsync(
        string email = "user@test.com",
        string password = "password123")
    {
        await CreateUserViaAdminAsync(email, password);
        return await LoginAsAsync(email, password);
    }
}
