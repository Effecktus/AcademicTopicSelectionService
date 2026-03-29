using System.Net;
using System.Net.Http.Json;
using AcademicTopicSelectionService.Application.Dictionaries.StudyGroups;
using AcademicTopicSelectionService.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace AcademicTopicSelectionService.IntegrationTests.Controllers;

[Collection(DatabaseCollection.CollectionName)]
public sealed class StudyGroupsControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "/api/v1/study-groups";

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public StudyGroupsControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
    }

    public async Task InitializeAsync() => await _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // GET /api/v1/study-groups
    // -------------------------------------------------------------------------

    [Fact]
    public async Task List_ReturnsEmptyPage_WhenNoGroupsExist()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(0);
        body.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task List_ReturnsAllGroups_WhenMultipleExist()
    {
        await CreateGroupAsync(4100);
        await CreateGroupAsync(4200);

        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(2);
        body.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task List_ReturnsFilteredGroup_WhenCodeNameMatches()
    {
        await CreateGroupAsync(4100);
        await CreateGroupAsync(4200);

        var response = await _client.GetAsync($"{BaseUrl}?codeName=4100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(1);
        body.Items[0].CodeName.Should().Be(4100);
    }

    [Fact]
    public async Task List_ReturnsEmpty_WhenCodeNameDoesNotMatch()
    {
        await CreateGroupAsync(4100);

        var response = await _client.GetAsync($"{BaseUrl}?codeName=9999");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(0);
    }

    [Fact]
    public async Task List_RespectsPageSize()
    {
        await CreateGroupAsync(4100);
        await CreateGroupAsync(4200);
        await CreateGroupAsync(4300);

        var response = await _client.GetAsync($"{BaseUrl}?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListResponse>();
        body!.Total.Should().Be(3);
        body.Items.Should().HaveCount(2);
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/study-groups/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Get_ReturnsGroup_WhenExists()
    {
        var created = await CreateGroupAsync(4411);

        var response = await _client.GetAsync($"{BaseUrl}/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<StudyGroupDto>();
        body!.Id.Should().Be(created.Id);
        body.CodeName.Should().Be(4411);
    }

    [Fact]
    public async Task Get_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // POST /api/v1/study-groups
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Create_Returns201_WhenDataIsValid()
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { CodeName = 4411 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<StudyGroupDto>();
        body!.CodeName.Should().Be(4411);
        response.Headers.Location.Should().NotBeNull();
    }

    [Theory]
    [InlineData(999)]
    [InlineData(10000)]
    [InlineData(0)]
    public async Task Create_Returns400_WhenCodeNameOutOfRange(int codeName)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { CodeName = codeName });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns409_WhenCodeNameAlreadyExists()
    {
        await CreateGroupAsync(4411);

        var response = await _client.PostAsJsonAsync(BaseUrl, new { CodeName = 4411 });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // -------------------------------------------------------------------------
    // PUT /api/v1/study-groups/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Update_Returns200_WhenDataIsValid()
    {
        var created = await CreateGroupAsync(4411);

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { CodeName = 4412 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<StudyGroupDto>();
        body!.CodeName.Should().Be(4412);
    }

    [Fact]
    public async Task Update_Returns404_WhenNotFound()
    {
        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{Guid.NewGuid()}",
            new { CodeName = 4411 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Returns409_WhenCodeNameTakenByOtherGroup()
    {
        var group1 = await CreateGroupAsync(4411);
        var group2 = await CreateGroupAsync(4412);

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{group2!.Id}",
            new { CodeName = 4411 });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData(999)]
    [InlineData(10000)]
    public async Task Update_Returns400_WhenCodeNameOutOfRange(int codeName)
    {
        var created = await CreateGroupAsync(4411);

        var response = await _client.PutAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { CodeName = codeName });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // PATCH /api/v1/study-groups/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Patch_Returns200_WhenCodeNameProvided()
    {
        var created = await CreateGroupAsync(4411);

        var response = await _client.PatchAsJsonAsync(
            $"{BaseUrl}/{created!.Id}",
            new { CodeName = 4412 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<StudyGroupDto>();
        body!.CodeName.Should().Be(4412);
    }

    [Fact]
    public async Task Patch_Returns400_WhenNoFieldsProvided()
    {
        var created = await CreateGroupAsync(4411);

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
            new { CodeName = 4411 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/v1/study-groups/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_Returns204_WhenGroupExists()
    {
        var created = await CreateGroupAsync(4411);

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
    public async Task Delete_RemovesGroup_SoSubsequentGetReturns404()
    {
        var created = await CreateGroupAsync(4411);
        await _client.DeleteAsync($"{BaseUrl}/{created!.Id}");

        var response = await _client.GetAsync($"{BaseUrl}/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<StudyGroupDto?> CreateGroupAsync(int codeName)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, new { CodeName = codeName });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StudyGroupDto>();
    }

    private sealed record ListResponse(int Page, int PageSize, long Total, StudyGroupDto[] Items);
}
