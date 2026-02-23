using System.Net;
using System.Net.Http.Json;
using DirectoryOfGraduates.Application.Dictionaries.UserRoles;
using DirectoryOfGraduates.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace DirectoryOfGraduates.IntegrationTests.Controllers;

[Collection(DatabaseCollection.CollectionName)]
public sealed class UserRolesControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "/api/v1/user-roles";

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public UserRolesControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public async Task InitializeAsync() => await _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // GET /api/v1/user-roles
    // -------------------------------------------------------------------------

    [Fact]
    public async Task List_ReturnsEmptyPage_WhenNoRolesExist()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(0);
        body.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task List_ReturnsAllRoles_WhenMultipleExist()
    {
        await CreateRoleAsync("Student", "Студент");
        await CreateRoleAsync("Teacher", "Преподаватель");

        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(2);
        body.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task List_ReturnsFilteredRoles_WhenSearchStringMatches()
    {
        await CreateRoleAsync("Student", "Студент");
        await CreateRoleAsync("Teacher", "Преподаватель");

        var response = await _client.GetAsync($"{BaseUrl}?searchString=Student");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(1);
        body.Items[0].Name.Should().Be("Student");
    }

    [Fact]
    public async Task List_RespectsPageSize()
    {
        await CreateRoleAsync("Student", "Студент");
        await CreateRoleAsync("Teacher", "Преподаватель");
        await CreateRoleAsync("Admin", "Администратор");

        var response = await _client.GetAsync($"{BaseUrl}?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(3);
        body.Items.Should().HaveCount(2);
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/user-roles/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Get_ReturnsRole_WhenExists()
    {
        var created = await CreateRoleAsync("Student", "Студент");

        var response = await _client.GetAsync($"{BaseUrl}/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserRoleDto>();
        body!.Id.Should().Be(created.Id);
        body.Name.Should().Be("Student");
    }

    [Fact]
    public async Task Get_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // POST /api/v1/user-roles
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Create_Returns201_WhenDataIsValid()
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { Name = "Student", DisplayName = "Студент" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<UserRoleDto>();
        body!.Name.Should().Be("Student");
        body.DisplayName.Should().Be("Студент");
        response.Headers.Location.Should().NotBeNull();
    }

    [Theory]
    [InlineData("", "Студент")]
    [InlineData("Student", "")]
    public async Task Create_Returns400_WhenRequiredFieldIsEmpty(string name, string displayName)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { Name = name, DisplayName = displayName });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns409_WhenNameAlreadyExists()
    {
        await CreateRoleAsync("Student", "Студент");

        var response = await _client.PostAsJsonAsync(BaseUrl, new { Name = "Student", DisplayName = "Другой студент" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // -------------------------------------------------------------------------
    // PUT /api/v1/user-roles/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Update_Returns200_WhenDataIsValid()
    {
        var created = await CreateRoleAsync("Student", "Студент");

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { Name = "Student", DisplayName = "Обновлённый студент" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserRoleDto>();
        body!.DisplayName.Should().Be("Обновлённый студент");
    }

    [Fact]
    public async Task Update_Returns404_WhenNotFound()
    {
        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{Guid.NewGuid()}",
            new { Name = "Student", DisplayName = "Студент" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Returns409_WhenNameTakenByOtherRole()
    {
        var role1 = await CreateRoleAsync("Student", "Студент");
        var role2 = await CreateRoleAsync("Teacher", "Преподаватель");

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{role2!.Id}",
            new { Name = "Student", DisplayName = "Преподаватель" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_Returns400_WhenNameIsEmpty()
    {
        var created = await CreateRoleAsync("Student", "Студент");

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { Name = "", DisplayName = "Студент" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // PATCH /api/v1/user-roles/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Patch_Returns200_WhenOnlyDisplayNameProvided()
    {
        var created = await CreateRoleAsync("Student", "Студент");

        var response = await _client.PatchAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { DisplayName = "Новый студент" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserRoleDto>();
        body!.DisplayName.Should().Be("Новый студент");
        body.Name.Should().Be("Student");
    }

    [Fact]
    public async Task Patch_Returns400_WhenNoFieldsProvided()
    {
        var created = await CreateRoleAsync("Student", "Студент");

        var response = await _client.PatchAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_Returns404_WhenNotFound()
    {
        var response = await _client.PatchAsJsonAsync(
            $"{BaseUrl}/{Guid.NewGuid()}",
            new { DisplayName = "Студент" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/v1/user-roles/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_Returns204_WhenRoleExists()
    {
        var created = await CreateRoleAsync("Student", "Студент");

        var response = await _client.DeleteAsync($"{BaseUrl}/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_Returns404_WhenNotFound()
    {
        var response = await _client.DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_RemovesRole_SoSubsequentGetReturns404()
    {
        var created = await CreateRoleAsync("Student", "Студент");
        await _client.DeleteAsync($"{BaseUrl}/{created!.Id}");

        var response = await _client.GetAsync($"{BaseUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<UserRoleDto?> CreateRoleAsync(string name, string displayName)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { Name = name, DisplayName = displayName });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserRoleDto>();
    }

    private sealed record ListResponse(int Page, int PageSize, long Total, UserRoleDto[] Items);
}
