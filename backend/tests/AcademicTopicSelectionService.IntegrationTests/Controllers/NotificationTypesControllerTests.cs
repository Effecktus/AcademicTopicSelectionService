using System.Net;
using System.Net.Http.Json;
using AcademicTopicSelectionService.Application.Dictionaries.NotificationTypes;
using AcademicTopicSelectionService.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace AcademicTopicSelectionService.IntegrationTests.Controllers;

[Collection(DatabaseCollection.CollectionName)]
public sealed class NotificationTypesControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "/api/v1/notification-types";

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public NotificationTypesControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public async Task InitializeAsync() => await _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task List_ReturnsEmptyPage_WhenNoTypesExist()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(0);
        body.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task List_ReturnsAllTypes_WhenMultipleExist()
    {
        await CreateTypeAsync("Info", "Информация");
        await CreateTypeAsync("Warning", "Предупреждение");

        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(2);
        body.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task List_ReturnsFilteredTypes_WhenSearchStringMatches()
    {
        await CreateTypeAsync("Info", "Информация");
        await CreateTypeAsync("Warning", "Предупреждение");

        var response = await _client.GetAsync($"{BaseUrl}?searchString=Info");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(1);
        body.Items[0].CodeName.Should().Be("Info");
    }

    [Fact]
    public async Task List_RespectsPageSize()
    {
        await CreateTypeAsync("Info", "Информация");
        await CreateTypeAsync("Warning", "Предупреждение");
        await CreateTypeAsync("Error", "Ошибка");

        var response = await _client.GetAsync($"{BaseUrl}?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(3);
        body.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Get_ReturnsType_WhenExists()
    {
        var created = await CreateTypeAsync("Info", "Информация");

        var response = await _client.GetAsync($"{BaseUrl}/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<NotificationTypeDto>();
        body!.Id.Should().Be(created.Id);
        body.CodeName.Should().Be("Info");
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
        var response = await _client.PostAsJsonAsync(BaseUrl, new { CodeName = "Info", DisplayName = "Информация" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<NotificationTypeDto>();
        body!.CodeName.Should().Be("Info");
        body.DisplayName.Should().Be("Информация");
        response.Headers.Location.Should().NotBeNull();
    }

    [Theory]
    [InlineData("", "Информация")]
    [InlineData("Info", "")]
    public async Task Create_Returns400_WhenRequiredFieldIsEmpty(string name, string displayName)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { CodeName = name, DisplayName = displayName });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns409_WhenNameAlreadyExists()
    {
        await CreateTypeAsync("Info", "Информация");

        var response = await _client.PostAsJsonAsync(BaseUrl, new { CodeName = "Info", DisplayName = "Другой тип" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_Returns200_WhenDataIsValid()
    {
        var created = await CreateTypeAsync("Info", "Информация");

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { CodeName = "Info", DisplayName = "Обновлённый тип" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<NotificationTypeDto>();
        body!.DisplayName.Should().Be("Обновлённый тип");
    }

    [Fact]
    public async Task Update_Returns404_WhenNotFound()
    {
        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{Guid.NewGuid()}",
            new { CodeName = "Info", DisplayName = "Информация" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Returns409_WhenNameTakenByOtherType()
    {
        var type1 = await CreateTypeAsync("Info", "Информация");
        var type2 = await CreateTypeAsync("Warning", "Предупреждение");

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{type2!.Id}",
            new { CodeName = "Info", DisplayName = "Предупреждение" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_Returns400_WhenNameIsEmpty()
    {
        var created = await CreateTypeAsync("Info", "Информация");

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { CodeName = "", DisplayName = "Информация" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_Returns200_WhenOnlyDisplayNameProvided()
    {
        var created = await CreateTypeAsync("Info", "Информация");

        var response = await _client.PatchAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { DisplayName = "Новый тип" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<NotificationTypeDto>();
        body!.DisplayName.Should().Be("Новый тип");
        body.CodeName.Should().Be("Info");
    }

    [Fact]
    public async Task Patch_Returns400_WhenNoFieldsProvided()
    {
        var created = await CreateTypeAsync("Info", "Информация");

        var response = await _client.PatchAsJsonAsync($"{BaseUrl}/{created!.Id}", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_Returns404_WhenNotFound()
    {
        var response = await _client.PatchAsJsonAsync(
            $"{BaseUrl}/{Guid.NewGuid()}",
            new { DisplayName = "Информация" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Returns204_WhenTypeExists()
    {
        var created = await CreateTypeAsync("Info", "Информация");

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
    public async Task Delete_RemovesType_SoSubsequentGetReturns404()
    {
        var created = await CreateTypeAsync("Info", "Информация");
        await _client.DeleteAsync($"{BaseUrl}/{created!.Id}");

        var response = await _client.GetAsync($"{BaseUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<NotificationTypeDto?> CreateTypeAsync(string name, string displayName)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { CodeName = name, DisplayName = displayName });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<NotificationTypeDto>();
    }

    private sealed record ListResponse(int Page, int PageSize, long Total, NotificationTypeDto[] Items);
}
