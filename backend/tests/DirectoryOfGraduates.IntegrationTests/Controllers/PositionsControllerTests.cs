using System.Net;
using System.Net.Http.Json;
using DirectoryOfGraduates.Application.Dictionaries.Positions;
using DirectoryOfGraduates.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace DirectoryOfGraduates.IntegrationTests.Controllers;

[Collection(DatabaseCollection.CollectionName)]
public sealed class PositionsControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "/api/v1/positions";

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public PositionsControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public async Task InitializeAsync() => await _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // GET /api/v1/positions
    // -------------------------------------------------------------------------

    [Fact]
    public async Task List_ReturnsEmptyPage_WhenNoPositionsExist()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(0);
        body.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task List_ReturnsAllPositions_WhenMultipleExist()
    {
        await CreatePositionAsync("Professor", "Профессор");
        await CreatePositionAsync("AssociateProfessor", "Доцент");

        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(2);
        body.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task List_ReturnsFilteredPositions_WhenSearchStringMatches()
    {
        await CreatePositionAsync("Professor", "Профессор");
        await CreatePositionAsync("AssociateProfessor", "Доцент");

        var response = await _client.GetAsync($"{BaseUrl}?searchString=Доцент");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(1);
        body.Items[0].Name.Should().Be("AssociateProfessor");
        body.Items[0].DisplayName.Should().Be("Доцент");
    }

    [Fact]
    public async Task List_RespectsPageSize()
    {
        await CreatePositionAsync("Professor", "Профессор");
        await CreatePositionAsync("AssociateProfessor", "Доцент");
        await CreatePositionAsync("SeniorLecturer", "Старший преподаватель");

        var response = await _client.GetAsync($"{BaseUrl}?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(3);
        body.Items.Should().HaveCount(2);
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/positions/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Get_ReturnsPosition_WhenExists()
    {
        var created = await CreatePositionAsync("Professor", "Профессор");

        var response = await _client.GetAsync($"{BaseUrl}/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PositionDto>();
        body!.Id.Should().Be(created.Id);
        body.Name.Should().Be("Professor");
    }

    [Fact]
    public async Task Get_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // POST /api/v1/positions
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Create_Returns201_WhenDataIsValid()
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { Name = "Professor", DisplayName = "Профессор" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<PositionDto>();
        body!.Name.Should().Be("Professor");
        body.DisplayName.Should().Be("Профессор");
        response.Headers.Location.Should().NotBeNull();
    }

    [Theory]
    [InlineData("", "Профессор")]
    [InlineData("Professor", "")]
    public async Task Create_Returns400_WhenRequiredFieldIsEmpty(string name, string displayName)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { Name = name, DisplayName = displayName });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns409_WhenNameAlreadyExists()
    {
        await CreatePositionAsync("Professor", "Профессор");

        var response = await _client.PostAsJsonAsync(BaseUrl, new { Name = "Professor", DisplayName = "Другой профессор" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // -------------------------------------------------------------------------
    // PUT /api/v1/positions/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Update_Returns200_WhenDataIsValid()
    {
        var created = await CreatePositionAsync("Professor", "Профессор");

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { Name = "Professor", DisplayName = "Обновлённый профессор" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PositionDto>();
        body!.DisplayName.Should().Be("Обновлённый профессор");
    }

    [Fact]
    public async Task Update_Returns404_WhenNotFound()
    {
        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{Guid.NewGuid()}",
            new { Name = "Professor", DisplayName = "Профессор" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Returns409_WhenNameTakenByOtherPosition()
    {
        var pos1 = await CreatePositionAsync("Professor", "Профессор");
        var pos2 = await CreatePositionAsync("AssociateProfessor", "Доцент");

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{pos2!.Id}",
            new { Name = "Professor", DisplayName = "Доцент" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_Returns400_WhenNameIsEmpty()
    {
        var created = await CreatePositionAsync("Professor", "Профессор");

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { Name = "", DisplayName = "Профессор" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // PATCH /api/v1/positions/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Patch_Returns200_WhenOnlyDisplayNameProvided()
    {
        var created = await CreatePositionAsync("Professor", "Профессор");

        var response = await _client.PatchAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { DisplayName = "Новый профессор" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PositionDto>();
        body!.DisplayName.Should().Be("Новый профессор");
        body.Name.Should().Be("Professor");
    }

    [Fact]
    public async Task Patch_Returns400_WhenNoFieldsProvided()
    {
        var created = await CreatePositionAsync("Professor", "Профессор");

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
            new { DisplayName = "Профессор" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/v1/positions/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_Returns204_WhenPositionExists()
    {
        var created = await CreatePositionAsync("Professor", "Профессор");

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
    public async Task Delete_RemovesPosition_SoSubsequentGetReturns404()
    {
        var created = await CreatePositionAsync("Professor", "Профессор");
        await _client.DeleteAsync($"{BaseUrl}/{created!.Id}");

        var response = await _client.GetAsync($"{BaseUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<PositionDto?> CreatePositionAsync(string name, string displayName)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { Name = name, DisplayName = displayName });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PositionDto>();
    }

    private sealed record ListResponse(int Page, int PageSize, long Total, PositionDto[] Items);
}
