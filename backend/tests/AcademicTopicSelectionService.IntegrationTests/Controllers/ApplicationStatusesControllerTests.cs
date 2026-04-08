using System.Net;
using System.Net.Http.Json;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.Application.Dictionaries.ApplicationStatuses;
using AcademicTopicSelectionService.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace AcademicTopicSelectionService.IntegrationTests.Controllers;

[Collection(DatabaseCollection.CollectionName)]
public sealed class ApplicationStatusesControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "/api/v1/application-statuses";

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public ApplicationStatusesControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient(AppRoles.Admin);
    }

    public async Task InitializeAsync() => await _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // GET /api/v1/application-statuses
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
        await CreateStatusAsync("Pending", "На рассмотрении");
        await CreateStatusAsync("Approved", "Одобрено");

        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(2);
        body.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task List_ReturnsFilteredStatuses_WhenSearchStringMatches()
    {
        await CreateStatusAsync("Pending", "На рассмотрении");
        await CreateStatusAsync("Approved", "Одобрено");

        var response = await _client.GetAsync($"{BaseUrl}?searchString=Pending");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(1);
        body.Items[0].CodeName.Should().Be("Pending");
    }

    [Fact]
    public async Task List_RespectsPageSize()
    {
        await CreateStatusAsync("Pending", "На рассмотрении");
        await CreateStatusAsync("Approved", "Одобрено");
        await CreateStatusAsync("Rejected", "Отклонено");

        var response = await _client.GetAsync($"{BaseUrl}?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(3);
        body.Items.Should().HaveCount(2);
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/application-statuses/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Get_ReturnsStatus_WhenExists()
    {
        var created = await CreateStatusAsync("Pending", "На рассмотрении");

        var response = await _client.GetAsync($"{BaseUrl}/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApplicationStatusDto>();
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
    // POST /api/v1/application-statuses
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Create_Returns201_WhenDataIsValid()
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { CodeName = "Pending", DisplayName = "На рассмотрении" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApplicationStatusDto>();
        body!.CodeName.Should().Be("Pending");
        body.DisplayName.Should().Be("На рассмотрении");
        response.Headers.Location.Should().NotBeNull();
    }

    [Theory]
    [InlineData("", "На рассмотрении")]
    [InlineData("Pending", "")]
    public async Task Create_Returns400_WhenRequiredFieldIsEmpty(string name, string displayName)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { CodeName = name, DisplayName = displayName });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns409_WhenNameAlreadyExists()
    {
        await CreateStatusAsync("Pending", "На рассмотрении");

        var response = await _client.PostAsJsonAsync(BaseUrl, new { CodeName = "Pending", DisplayName = "Другой статус" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // -------------------------------------------------------------------------
    // PUT /api/v1/application-statuses/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Update_Returns200_WhenDataIsValid()
    {
        var created = await CreateStatusAsync("Pending", "На рассмотрении");

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { CodeName = "Pending", DisplayName = "Обновлённый статус" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApplicationStatusDto>();
        body!.DisplayName.Should().Be("Обновлённый статус");
    }

    [Fact]
    public async Task Update_Returns404_WhenNotFound()
    {
        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{Guid.NewGuid()}",
            new { CodeName = "Pending", DisplayName = "На рассмотрении" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Returns409_WhenNameTakenByOtherStatus()
    {
        var status1 = await CreateStatusAsync("Pending", "На рассмотрении");
        var status2 = await CreateStatusAsync("Approved", "Одобрено");

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{status2!.Id}",
            new { CodeName = "Pending", DisplayName = "Одобрено" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_Returns400_WhenNameIsEmpty()
    {
        var created = await CreateStatusAsync("Pending", "На рассмотрении");

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { CodeName = "", DisplayName = "На рассмотрении" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // PATCH /api/v1/application-statuses/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Patch_Returns200_WhenOnlyDisplayNameProvided()
    {
        var created = await CreateStatusAsync("Pending", "На рассмотрении");

        var response = await _client.PatchAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { DisplayName = "Новый статус" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApplicationStatusDto>();
        body!.DisplayName.Should().Be("Новый статус");
        body.CodeName.Should().Be("Pending");
    }

    [Fact]
    public async Task Patch_Returns400_WhenNoFieldsProvided()
    {
        var created = await CreateStatusAsync("Pending", "На рассмотрении");

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
            new { DisplayName = "На рассмотрении" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/v1/application-statuses/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_Returns204_WhenStatusExists()
    {
        var created = await CreateStatusAsync("Pending", "На рассмотрении");

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
        var created = await CreateStatusAsync("Pending", "На рассмотрении");
        await _client.DeleteAsync($"{BaseUrl}/{created!.Id}");

        var response = await _client.GetAsync($"{BaseUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<ApplicationStatusDto?> CreateStatusAsync(string name, string displayName)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { CodeName = name, DisplayName = displayName });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ApplicationStatusDto>();
    }

    private sealed record ListResponse(int Page, int PageSize, long Total, ApplicationStatusDto[] Items);
}
