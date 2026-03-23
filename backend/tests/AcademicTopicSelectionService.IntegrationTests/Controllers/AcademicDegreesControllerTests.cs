using System.Net;
using System.Net.Http.Json;
using AcademicTopicSelectionService.Application.Dictionaries.AcademicDegrees;
using AcademicTopicSelectionService.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace AcademicTopicSelectionService.IntegrationTests.Controllers;

[Collection(DatabaseCollection.CollectionName)]
public sealed class AcademicDegreesControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "/api/v1/academic-degrees";

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public AcademicDegreesControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public async Task InitializeAsync() => await _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task List_ReturnsEmptyPage_WhenNoDegreesExist()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(0);
        body.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task List_ReturnsAllDegrees_WhenMultipleExist()
    {
        await CreateDegreeAsync("Candidate", "Кандидат наук", "канд. наук");
        await CreateDegreeAsync("Doctor", "Доктор наук", "д-р наук");

        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(2);
        body.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task List_ReturnsFilteredDegrees_WhenSearchStringMatches()
    {
        await CreateDegreeAsync("Candidate", "Кандидат наук", "канд. наук");
        await CreateDegreeAsync("Doctor", "Доктор наук", "д-р наук");

        var response = await _client.GetAsync($"{BaseUrl}?searchString=Candidate");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(1);
        body.Items[0].Name.Should().Be("Candidate");
    }

    [Fact]
    public async Task List_RespectsPageSize()
    {
        await CreateDegreeAsync("Candidate", "Кандидат наук", "канд. наук");
        await CreateDegreeAsync("Doctor", "Доктор наук", "д-р наук");
        await CreateDegreeAsync("None", "Без степени", null);

        var response = await _client.GetAsync($"{BaseUrl}?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(3);
        body.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Get_ReturnsDegree_WhenExists()
    {
        var created = await CreateDegreeAsync("Candidate", "Кандидат наук", "канд. наук");

        var response = await _client.GetAsync($"{BaseUrl}/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AcademicDegreeDto>();
        body!.Id.Should().Be(created.Id);
        body.Name.Should().Be("Candidate");
        body.ShortName.Should().Be("канд. наук");
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
        var response = await _client.PostAsJsonAsync(BaseUrl, new { Name = "Candidate", DisplayName = "Кандидат наук", ShortName = "канд. наук" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AcademicDegreeDto>();
        body!.Name.Should().Be("Candidate");
        body.DisplayName.Should().Be("Кандидат наук");
        body.ShortName.Should().Be("канд. наук");
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_Returns201_WhenShortNameIsNull()
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { Name = "None", DisplayName = "Без степени" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AcademicDegreeDto>();
        body!.ShortName.Should().BeNull();
    }

    [Theory]
    [InlineData("", "Кандидат наук")]
    [InlineData("Candidate", "")]
    public async Task Create_Returns400_WhenRequiredFieldIsEmpty(string name, string displayName)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { Name = name, DisplayName = displayName });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns409_WhenNameAlreadyExists()
    {
        await CreateDegreeAsync("Candidate", "Кандидат наук", "канд. наук");

        var response = await _client.PostAsJsonAsync(BaseUrl, new { Name = "Candidate", DisplayName = "Другой тип" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_Returns200_WhenDataIsValid()
    {
        var created = await CreateDegreeAsync("Candidate", "Кандидат наук", "канд. наук");

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { Name = "Candidate", DisplayName = "Обновлённая степень", ShortName = "канд." });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AcademicDegreeDto>();
        body!.DisplayName.Should().Be("Обновлённая степень");
        body.ShortName.Should().Be("канд.");
    }

    [Fact]
    public async Task Update_Returns404_WhenNotFound()
    {
        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{Guid.NewGuid()}",
            new { Name = "Candidate", DisplayName = "Кандидат наук" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Returns409_WhenNameTakenByOtherDegree()
    {
        await CreateDegreeAsync("Candidate", "Кандидат наук", "канд. наук");
        var degree2 = await CreateDegreeAsync("Doctor", "Доктор наук", "д-р наук");

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{degree2!.Id}",
            new { Name = "Candidate", DisplayName = "Доктор наук", ShortName = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_Returns400_WhenNameIsEmpty()
    {
        var created = await CreateDegreeAsync("Candidate", "Кандидат наук", "канд. наук");

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { Name = "", DisplayName = "Кандидат наук" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_Returns200_WhenOnlyDisplayNameProvided()
    {
        var created = await CreateDegreeAsync("Candidate", "Кандидат наук", "канд. наук");

        var response = await _client.PatchAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { DisplayName = "Новая степень" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AcademicDegreeDto>();
        body!.DisplayName.Should().Be("Новая степень");
        body.Name.Should().Be("Candidate");
    }

    [Fact]
    public async Task Patch_Returns400_WhenNoFieldsProvided()
    {
        var created = await CreateDegreeAsync("Candidate", "Кандидат наук", "канд. наук");

        var response = await _client.PatchAsJsonAsync($"{BaseUrl}/{created!.Id}", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_Returns404_WhenNotFound()
    {
        var response = await _client.PatchAsJsonAsync(
            $"{BaseUrl}/{Guid.NewGuid()}",
            new { DisplayName = "Кандидат наук" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Returns204_WhenDegreeExists()
    {
        var created = await CreateDegreeAsync("Candidate", "Кандидат наук", "канд. наук");

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
    public async Task Delete_RemovesDegree_SoSubsequentGetReturns404()
    {
        var created = await CreateDegreeAsync("Candidate", "Кандидат наук", "канд. наук");
        await _client.DeleteAsync($"{BaseUrl}/{created!.Id}");

        var response = await _client.GetAsync($"{BaseUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<AcademicDegreeDto?> CreateDegreeAsync(string name, string displayName, string? shortName = null)
    {
        var payload = shortName is null
            ? (object)new { Name = name, DisplayName = displayName }
            : new { Name = name, DisplayName = displayName, ShortName = shortName };
        var response = await _client.PostAsJsonAsync(BaseUrl, payload);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AcademicDegreeDto>();
    }

    private sealed record ListResponse(int Page, int PageSize, long Total, AcademicDegreeDto[] Items);
}
