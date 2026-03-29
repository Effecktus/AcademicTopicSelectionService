using System.Net;
using System.Net.Http.Json;
using AcademicTopicSelectionService.Application.Dictionaries.ApplicationActionStatuses;
using AcademicTopicSelectionService.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace AcademicTopicSelectionService.IntegrationTests.Controllers;

[Collection(DatabaseCollection.CollectionName)]
public sealed class ApplicationActionStatusesControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "/api/v1/application-action-statuses";

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public ApplicationActionStatusesControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public async Task InitializeAsync() => await _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // GET /api/v1/application-action-statuses
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
        await CreateStatusAsync("Pending", "На согласовании");
        await CreateStatusAsync("Approved", "Согласовано");

        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(2);
        body.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task List_ReturnsFilteredStatuses_WhenSearchStringMatches()
    {
        await CreateStatusAsync("Pending", "На согласовании");
        await CreateStatusAsync("Approved", "Согласовано");

        var response = await _client.GetAsync($"{BaseUrl}?searchString=Pending");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(1);
        body.Items[0].CodeName.Should().Be("Pending");
    }

    [Fact]
    public async Task List_RespectsPageSize()
    {
        await CreateStatusAsync("Pending", "На согласовании");
        await CreateStatusAsync("Approved", "Согласовано");
        await CreateStatusAsync("Rejected", "Отклонено");

        var response = await _client.GetAsync($"{BaseUrl}?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(3);
        body.Items.Should().HaveCount(2);
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/application-action-statuses/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Get_ReturnsStatus_WhenExists()
    {
        var created = await CreateStatusAsync("Pending", "На согласовании");

        var response = await _client.GetAsync($"{BaseUrl}/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApplicationActionStatusDto>();
        body!.Id.Should().Be(created.Id);
        body.CodeName.Should().Be("Pending");
    }

    [Fact]
    public async Task Get_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // POST /api/v1/application-action-statuses
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Create_Returns201_WhenDataIsValid()
    {
        var response = await _client.PostAsJsonAsync(BaseUrl,
            new { CodeName = "Pending", DisplayName = "На согласовании" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApplicationActionStatusDto>();
        body!.CodeName.Should().Be("Pending");
        body.DisplayName.Should().Be("На согласовании");
        response.Headers.Location.Should().NotBeNull();
    }

    [Theory]
    [InlineData("", "На согласовании")]
    [InlineData("Pending", "")]
    public async Task Create_Returns400_WhenRequiredFieldIsEmpty(string codeName, string displayName)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl,
            new { CodeName = codeName, DisplayName = displayName });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns409_WhenCodeNameAlreadyExists()
    {
        await CreateStatusAsync("Pending", "На согласовании");

        var response = await _client.PostAsJsonAsync(BaseUrl,
            new { CodeName = "Pending", DisplayName = "Другой статус" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // -------------------------------------------------------------------------
    // PUT /api/v1/application-action-statuses/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Update_Returns200_WhenDataIsValid()
    {
        var created = await CreateStatusAsync("Pending", "На согласовании");

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{created!.Id}",
            new { CodeName = "Pending", DisplayName = "Обновлённый статус" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApplicationActionStatusDto>();
        body!.DisplayName.Should().Be("Обновлённый статус");
    }

    [Fact]
    public async Task Update_Returns404_WhenNotFound()
    {
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{Guid.NewGuid()}",
            new { CodeName = "Pending", DisplayName = "На согласовании" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Returns409_WhenCodeNameTakenByOtherStatus()
    {
        var status1 = await CreateStatusAsync("Pending", "На согласовании");
        var status2 = await CreateStatusAsync("Approved", "Согласовано");

        var response = await _client.PutAsJsonAsync($"{BaseUrl}/{status2!.Id}",
            new { CodeName = "Pending", DisplayName = "Согласовано" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // -------------------------------------------------------------------------
    // PATCH /api/v1/application-action-statuses/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Patch_Returns200_WhenOnlyDisplayNameProvided()
    {
        var created = await CreateStatusAsync("Pending", "На согласовании");

        var response = await _client.PatchAsJsonAsync($"{BaseUrl}/{created!.Id}",
            new { DisplayName = "Новое имя" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApplicationActionStatusDto>();
        body!.DisplayName.Should().Be("Новое имя");
        body.CodeName.Should().Be("Pending");
    }

    [Fact]
    public async Task Patch_Returns400_WhenNoFieldsProvided()
    {
        var created = await CreateStatusAsync("Pending", "На согласовании");

        var response = await _client.PatchAsJsonAsync($"{BaseUrl}/{created!.Id}", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_Returns404_WhenNotFound()
    {
        var response = await _client.PatchAsJsonAsync($"{BaseUrl}/{Guid.NewGuid()}",
            new { DisplayName = "На согласовании" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/v1/application-action-statuses/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_Returns204_WhenStatusExists()
    {
        var created = await CreateStatusAsync("Pending", "На согласовании");

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
        var created = await CreateStatusAsync("Pending", "На согласовании");
        await _client.DeleteAsync($"{BaseUrl}/{created!.Id}");

        var response = await _client.GetAsync($"{BaseUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<ApplicationActionStatusDto?> CreateStatusAsync(string codeName, string displayName)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl,
            new { CodeName = codeName, DisplayName = displayName });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ApplicationActionStatusDto>();
    }

    private sealed record ListResponse(int Page, int PageSize, long Total, ApplicationActionStatusDto[] Items);
}
