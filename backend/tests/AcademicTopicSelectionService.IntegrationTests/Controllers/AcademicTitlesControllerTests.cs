using System.Net;
using System.Net.Http.Json;
using AcademicTopicSelectionService.Application.Dictionaries.AcademicTitles;
using AcademicTopicSelectionService.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace AcademicTopicSelectionService.IntegrationTests.Controllers;

[Collection(DatabaseCollection.CollectionName)]
public sealed class AcademicTitlesControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "/api/v1/academic-titles";

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public AcademicTitlesControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public async Task InitializeAsync() => await _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task List_ReturnsEmptyPage_WhenNoTitlesExist()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(0);
        body.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task List_ReturnsAllTitles_WhenMultipleExist()
    {
        await CreateTitleAsync("AssociateProfessor", "Доцент");
        await CreateTitleAsync("Professor", "Профессор");

        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(2);
        body.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task List_ReturnsFilteredTitles_WhenSearchStringMatches()
    {
        await CreateTitleAsync("AssociateProfessor", "Доцент");
        await CreateTitleAsync("Professor", "Профессор");

        var response = await _client.GetAsync($"{BaseUrl}?searchString=Associate");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(1);
        body.Items[0].Name.Should().Be("AssociateProfessor");
    }

    [Fact]
    public async Task List_RespectsPageSize()
    {
        await CreateTitleAsync("AssociateProfessor", "Доцент");
        await CreateTitleAsync("Professor", "Профессор");
        await CreateTitleAsync("Assistant", "Ассистент");

        var response = await _client.GetAsync($"{BaseUrl}?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(3);
        body.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Get_ReturnsTitle_WhenExists()
    {
        var created = await CreateTitleAsync("AssociateProfessor", "Доцент");

        var response = await _client.GetAsync($"{BaseUrl}/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AcademicTitleDto>();
        body!.Id.Should().Be(created.Id);
        body.Name.Should().Be("AssociateProfessor");
    }

    [Fact]
    public async Task Get_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Returns201_WhenDataIsValid()
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { Name = "AssociateProfessor", DisplayName = "Доцент" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AcademicTitleDto>();
        body!.Name.Should().Be("AssociateProfessor");
        body.DisplayName.Should().Be("Доцент");
        response.Headers.Location.Should().NotBeNull();
    }

    [Theory]
    [InlineData("", "Доцент")]
    [InlineData("AssociateProfessor", "")]
    public async Task Create_Returns400_WhenRequiredFieldIsEmpty(string name, string displayName)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { Name = name, DisplayName = displayName });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns409_WhenNameAlreadyExists()
    {
        await CreateTitleAsync("AssociateProfessor", "Доцент");

        var response = await _client.PostAsJsonAsync(BaseUrl, new { Name = "AssociateProfessor", DisplayName = "Другой тип" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_Returns200_WhenDataIsValid()
    {
        var created = await CreateTitleAsync("AssociateProfessor", "Доцент");

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { Name = "AssociateProfessor", DisplayName = "Обновлённое звание" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AcademicTitleDto>();
        body!.DisplayName.Should().Be("Обновлённое звание");
    }

    [Fact]
    public async Task Update_Returns404_WhenNotFound()
    {
        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{Guid.NewGuid()}",
            new { Name = "AssociateProfessor", DisplayName = "Доцент" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Returns409_WhenNameTakenByOtherTitle()
    {
        await CreateTitleAsync("AssociateProfessor", "Доцент");
        var title2 = await CreateTitleAsync("Professor", "Профессор");

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{title2!.Id}",
            new { Name = "AssociateProfessor", DisplayName = "Профессор" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_Returns400_WhenNameIsEmpty()
    {
        var created = await CreateTitleAsync("AssociateProfessor", "Доцент");

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { Name = "", DisplayName = "Доцент" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_Returns200_WhenOnlyDisplayNameProvided()
    {
        var created = await CreateTitleAsync("AssociateProfessor", "Доцент");

        var response = await _client.PatchAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { DisplayName = "Новое звание" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AcademicTitleDto>();
        body!.DisplayName.Should().Be("Новое звание");
        body.Name.Should().Be("AssociateProfessor");
    }

    [Fact]
    public async Task Patch_Returns400_WhenNoFieldsProvided()
    {
        var created = await CreateTitleAsync("AssociateProfessor", "Доцент");

        var response = await _client.PatchAsJsonAsync($"{BaseUrl}/{created!.Id}", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_Returns404_WhenNotFound()
    {
        var response = await _client.PatchAsJsonAsync(
            $"{BaseUrl}/{Guid.NewGuid()}",
            new { DisplayName = "Доцент" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Returns204_WhenTitleExists()
    {
        var created = await CreateTitleAsync("AssociateProfessor", "Доцент");

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
    public async Task Delete_RemovesTitle_SoSubsequentGetReturns404()
    {
        var created = await CreateTitleAsync("AssociateProfessor", "Доцент");
        await _client.DeleteAsync($"{BaseUrl}/{created!.Id}");

        var response = await _client.GetAsync($"{BaseUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<AcademicTitleDto?> CreateTitleAsync(string name, string displayName)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { Name = name, DisplayName = displayName });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AcademicTitleDto>();
    }

    private sealed record ListResponse(int Page, int PageSize, long Total, AcademicTitleDto[] Items);
}
