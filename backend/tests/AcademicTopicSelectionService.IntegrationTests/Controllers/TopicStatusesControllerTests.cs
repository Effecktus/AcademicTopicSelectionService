using System.Net;
using System.Net.Http.Json;
using AcademicTopicSelectionService.Application.Dictionaries.TopicStatuses;
using AcademicTopicSelectionService.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace AcademicTopicSelectionService.IntegrationTests.Controllers;

[Collection(DatabaseCollection.CollectionName)]
public sealed class TopicStatusesControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "/api/v1/topic-statuses";

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public TopicStatusesControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public async Task InitializeAsync() => await _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // GET /api/v1/topic-statuses
    // -------------------------------------------------------------------------

    [Fact]
    public async Task List_ReturnsEmptyPage_WhenNoStatusesExist()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(0);
        body.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task List_ReturnsAllStatuses_WhenMultipleExist()
    {
        await CreateStatusAsync("Open", "Открыт");
        await CreateStatusAsync("Closed", "Закрыт");

        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(2);
        body.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task List_ReturnsFilteredStatuses_WhenSearchStringMatches()
    {
        await CreateStatusAsync("Open", "Открыт");
        await CreateStatusAsync("Closed", "Закрыт");

        var response = await _client.GetAsync($"{BaseUrl}?searchString=Open");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(1);
        body.Items[0].Name.Should().Be("Open");
    }

    [Fact]
    public async Task List_RespectsPageSize()
    {
        await CreateStatusAsync("Open", "Открыт");
        await CreateStatusAsync("Closed", "Закрыт");
        await CreateStatusAsync("Archived", "В архиве");

        var response = await _client.GetAsync($"{BaseUrl}?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(3);
        body.Items.Should().HaveCount(2);
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/topic-statuses/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Get_ReturnsStatus_WhenExists()
    {
        var created = await CreateStatusAsync("Open", "Открыт");

        var response = await _client.GetAsync($"{BaseUrl}/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TopicStatusDto>();
        body!.Id.Should().Be(created.Id);
        body.Name.Should().Be("Open");
    }

    [Fact]
    public async Task Get_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // POST /api/v1/topic-statuses
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Create_Returns201_WhenDataIsValid()
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { Name = "Open", DisplayName = "Открыт" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<TopicStatusDto>();
        body!.Name.Should().Be("Open");
        body.DisplayName.Should().Be("Открыт");
        response.Headers.Location.Should().NotBeNull();
    }

    [Theory]
    [InlineData("", "Открыт")]
    [InlineData("Open", "")]
    public async Task Create_Returns400_WhenRequiredFieldIsEmpty(string name, string displayName)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { Name = name, DisplayName = displayName });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns409_WhenNameAlreadyExists()
    {
        await CreateStatusAsync("Open", "Открыт");

        var response = await _client.PostAsJsonAsync(BaseUrl, new { Name = "Open", DisplayName = "Другой статус" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // -------------------------------------------------------------------------
    // PUT /api/v1/topic-statuses/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Update_Returns200_WhenDataIsValid()
    {
        var created = await CreateStatusAsync("Open", "Открыт");

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { Name = "Open", DisplayName = "Обновлённый статус" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TopicStatusDto>();
        body!.DisplayName.Should().Be("Обновлённый статус");
    }

    [Fact]
    public async Task Update_Returns404_WhenNotFound()
    {
        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{Guid.NewGuid()}",
            new { Name = "Open", DisplayName = "Открыт" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Returns409_WhenNameTakenByOtherStatus()
    {
        var status1 = await CreateStatusAsync("Open", "Открыт");
        var status2 = await CreateStatusAsync("Closed", "Закрыт");

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{status2!.Id}",
            new { Name = "Open", DisplayName = "Закрыт" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_Returns400_WhenNameIsEmpty()
    {
        var created = await CreateStatusAsync("Open", "Открыт");

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { Name = "", DisplayName = "Открыт" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // PATCH /api/v1/topic-statuses/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Patch_Returns200_WhenOnlyDisplayNameProvided()
    {
        var created = await CreateStatusAsync("Open", "Открыт");

        var response = await _client.PatchAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { DisplayName = "Новый статус" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TopicStatusDto>();
        body!.DisplayName.Should().Be("Новый статус");
        body.Name.Should().Be("Open");
    }

    [Fact]
    public async Task Patch_Returns400_WhenNoFieldsProvided()
    {
        var created = await CreateStatusAsync("Open", "Открыт");

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
            new { DisplayName = "Открыт" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/v1/topic-statuses/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_Returns204_WhenStatusExists()
    {
        var created = await CreateStatusAsync("Open", "Открыт");

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
    public async Task Delete_RemovesStatus_SoSubsequentGetReturns404()
    {
        var created = await CreateStatusAsync("Open", "Открыт");
        await _client.DeleteAsync($"{BaseUrl}/{created!.Id}");

        var response = await _client.GetAsync($"{BaseUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<TopicStatusDto?> CreateStatusAsync(string name, string displayName)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { Name = name, DisplayName = displayName });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TopicStatusDto>();
    }

    private sealed record ListResponse(int Page, int PageSize, long Total, TopicStatusDto[] Items);
}
