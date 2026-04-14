using System.Net;
using System.Net.Http.Json;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.Application.ApplicationActions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.ApplicationActionStatuses;
using AcademicTopicSelectionService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AcademicTopicSelectionService.IntegrationTests.Controllers;

[Collection(DatabaseCollection.CollectionName)]
public sealed class ApplicationActionsControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "/api/v1/application-actions";
    private const string ActionStatusesUrl = "/api/v1/application-action-statuses";
    private const string AppStatusesUrl = "/api/v1/application-statuses";

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;
    private readonly HttpClient _adminClient;

    public ApplicationActionsControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient(AppRoles.Student);
        _adminClient = fixture.CreateAuthenticatedClient(AppRoles.Admin);
    }

    public async Task InitializeAsync() => await _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // GET /api/v1/application-actions?applicationId=...
    // -------------------------------------------------------------------------

    [Fact]
    public async Task List_Returns400_WhenApplicationIdIsEmpty()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_ReturnsEmptyPage_WhenNoActionsExist()
    {
        var (appId, _) = await CreateApplicationWithSeedAsync();

        var response = await _client.GetAsync($"{BaseUrl}?applicationId={appId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<ApplicationActionDto>>();
        body!.Total.Should().Be(0);
        body.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task List_ReturnsActions_WhenExist()
    {
        var (appId, userId) = await CreateApplicationWithSeedAsync();
        var pendingStatusId = await CreateActionStatusAsync("Pending", "На согласовании");

        await CreateActionAsync(appId, userId);

        var response = await _client.GetAsync($"{BaseUrl}?applicationId={appId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<ApplicationActionDto>>();
        body!.Total.Should().Be(1);
        body.Items[0].ApplicationId.Should().Be(appId);
        _ = pendingStatusId;
    }

    // -------------------------------------------------------------------------
    // GET /api/v1/application-actions/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Get_ReturnsAction_WhenExists()
    {
        var (appId, userId) = await CreateApplicationWithSeedAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");
        var created = await CreateActionAsync(appId, userId);

        var response = await _client.GetAsync($"{BaseUrl}/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApplicationActionDto>();
        body!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task Get_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // POST /api/v1/application-actions
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Create_Returns201_WhenDataIsValid()
    {
        var (appId, userId) = await CreateApplicationWithSeedAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");

        var response = await _client.PostAsJsonAsync(BaseUrl,
            new { ApplicationId = appId, ResponsibleId = userId, Comment = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApplicationActionDto>();
        body!.ApplicationId.Should().Be(appId);
        body.ResponsibleId.Should().Be(userId);
        body.StatusCodeName.Should().Be("Pending");
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_Returns201_WithComment_WhenCommentProvided()
    {
        var (appId, userId) = await CreateApplicationWithSeedAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");

        var response = await _client.PostAsJsonAsync(BaseUrl,
            new { ApplicationId = appId, ResponsibleId = userId, Comment = "Комментарий" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApplicationActionDto>();
        body!.Comment.Should().Be("Комментарий");
    }

    [Fact]
    public async Task Create_Returns404_WhenApplicationDoesNotExist()
    {
        var (_, userId) = await CreateApplicationWithSeedAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");

        var response = await _client.PostAsJsonAsync(BaseUrl,
            new { ApplicationId = Guid.NewGuid(), ResponsibleId = userId, Comment = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Returns404_WhenResponsibleUserDoesNotExist()
    {
        var (appId, _) = await CreateApplicationWithSeedAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");

        var response = await _client.PostAsJsonAsync(BaseUrl,
            new { ApplicationId = appId, ResponsibleId = Guid.NewGuid(), Comment = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Returns400_WhenApplicationIdIsEmpty()
    {
        var (_, userId) = await CreateApplicationWithSeedAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");

        var response = await _client.PostAsJsonAsync(BaseUrl,
            new { ApplicationId = Guid.Empty, ResponsibleId = userId, Comment = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns400_WhenResponsibleIdIsEmpty()
    {
        var (appId, _) = await CreateApplicationWithSeedAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");

        var response = await _client.PostAsJsonAsync(BaseUrl,
            new { ApplicationId = appId, ResponsibleId = Guid.Empty, Comment = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns400_WhenCommentIsWhitespace()
    {
        var (appId, userId) = await CreateApplicationWithSeedAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");

        var response = await _client.PostAsJsonAsync(BaseUrl,
            new { ApplicationId = appId, ResponsibleId = userId, Comment = "   " });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // PATCH /api/v1/application-actions/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Update_Returns200_WhenStatusIsChanged()
    {
        var (appId, userId) = await CreateApplicationWithSeedAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");
        var approvedStatusId = await CreateActionStatusAsync("Approved", "Согласовано");
        var created = await CreateActionAsync(appId, userId);

        var response = await _client.PatchAsJsonAsync($"{BaseUrl}/{created!.Id}",
            new { StatusId = approvedStatusId, Comment = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApplicationActionDto>();
        body!.StatusId.Should().Be(approvedStatusId);
        body.StatusCodeName.Should().Be("Approved");
    }

    [Fact]
    public async Task Update_Returns200_WhenOnlyCommentIsChanged()
    {
        var (appId, userId) = await CreateApplicationWithSeedAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");
        var created = await CreateActionAsync(appId, userId);

        var response = await _client.PatchAsJsonAsync($"{BaseUrl}/{created!.Id}",
            new { StatusId = (Guid?)null, Comment = "Новый комментарий" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApplicationActionDto>();
        body!.Comment.Should().Be("Новый комментарий");
    }

    [Fact]
    public async Task Update_Returns400_WhenNoFieldsProvided()
    {
        var (appId, userId) = await CreateApplicationWithSeedAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");
        var created = await CreateActionAsync(appId, userId);

        var response = await _client.PatchAsJsonAsync($"{BaseUrl}/{created!.Id}",
            new { StatusId = (Guid?)null, Comment = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Returns404_WhenActionNotFound()
    {
        await CreateActionStatusAsync("Approved", "Согласовано");
        var approvedStatusId = await GetStatusIdByCodeNameAsync(ActionStatusesUrl, "Approved");

        var response = await _client.PatchAsJsonAsync($"{BaseUrl}/{Guid.NewGuid()}",
            new { StatusId = approvedStatusId, Comment = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Returns404_WhenStatusNotFound()
    {
        var (appId, userId) = await CreateApplicationWithSeedAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");
        var created = await CreateActionAsync(appId, userId);

        var response = await _client.PatchAsJsonAsync($"{BaseUrl}/{created!.Id}",
            new { StatusId = Guid.NewGuid(), Comment = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Returns400_WhenCommentIsWhitespace()
    {
        var (appId, userId) = await CreateApplicationWithSeedAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");
        var created = await CreateActionAsync(appId, userId);

        var response = await _client.PatchAsJsonAsync($"{BaseUrl}/{created!.Id}",
            new { StatusId = (Guid?)null, Comment = "   " });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/v1/application-actions/{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_Returns204_WhenActionExists()
    {
        var (appId, userId) = await CreateApplicationWithSeedAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");
        var created = await CreateActionAsync(appId, userId);

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
    public async Task Delete_RemovesAction_SoSubsequentGetReturns404()
    {
        var (appId, userId) = await CreateApplicationWithSeedAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");
        var created = await CreateActionAsync(appId, userId);

        await _client.DeleteAsync($"{BaseUrl}/{created!.Id}");
        var getResponse = await _client.GetAsync($"{BaseUrl}/{created.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Создаёт минимально необходимую цепочку: UserRole → User → ApplicationStatus → StudentApplication.
    /// Возвращает (applicationId, userId) для использования в тестах.
    /// </summary>
    private async Task<(Guid applicationId, Guid userId)> CreateApplicationWithSeedAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<AcademicTopicSelectionService.Infrastructure.Data.ApplicationDbContext>();

        var roleId = Guid.NewGuid();
        db.UserRoles.Add(new() { Id = roleId, CodeName = $"Teacher_{roleId:N}", DisplayName = "Преподаватель" });

        var userId = Guid.NewGuid();
        db.Users.Add(new()
        {
            Id = userId, Email = $"teacher_{userId:N}@test.com",
            PasswordHash = "hash", FirstName = "Иван", LastName = "Петров",
            RoleId = roleId, IsActive = true
        });

        var topicStatusId = Guid.NewGuid();
        db.TopicStatuses.Add(new() { Id = topicStatusId, CodeName = $"Active_{topicStatusId:N}", DisplayName = "Активна" });

        var topicCreatorTypeId = Guid.NewGuid();
        db.TopicCreatorTypes.Add(new() { Id = topicCreatorTypeId, CodeName = $"Teacher_{topicCreatorTypeId:N}", DisplayName = "Преподаватель" });

        var topicId = Guid.NewGuid();
        db.Topics.Add(new()
        {
            Id = topicId, Title = $"Тема {topicId:N}",
            StatusId = topicStatusId, CreatorTypeId = topicCreatorTypeId, CreatedBy = userId
        });

        var studyGroupId = Guid.NewGuid();
        db.StudyGroups.Add(new() { Id = studyGroupId, CodeName = 4001 });

        var studentUserId = Guid.NewGuid();
        db.Users.Add(new()
        {
            Id = studentUserId, Email = $"student_{studentUserId:N}@test.com",
            PasswordHash = "hash", FirstName = "Алексей", LastName = "Иванов",
            RoleId = roleId, IsActive = true
        });

        var studentId = Guid.NewGuid();
        db.Students.Add(new() { Id = studentId, UserId = studentUserId, GroupId = studyGroupId });

        var appStatusId = Guid.NewGuid();
        db.ApplicationStatuses.Add(new() { Id = appStatusId, CodeName = $"Pending_{appStatusId:N}", DisplayName = "Ожидает" });

        var applicationId = Guid.NewGuid();
        db.StudentApplications.Add(new()
        {
            Id = applicationId, StudentId = studentId,
            TopicId = topicId, StatusId = appStatusId
        });

        await db.SaveChangesAsync();
        return (applicationId, userId);
    }

    private async Task<Guid> CreateActionStatusAsync(string codeName, string displayName)
    {
        var response = await _adminClient.PostAsJsonAsync(ActionStatusesUrl,
            new { CodeName = codeName, DisplayName = displayName });

        if (!response.IsSuccessStatusCode)
        {
            var existing = await GetStatusIdByCodeNameAsync(ActionStatusesUrl, codeName);
            return existing;
        }

        var body = await response.Content.ReadFromJsonAsync<ApplicationActionStatusDto>();
        return body!.Id;
    }

    private async Task<Guid> GetStatusIdByCodeNameAsync(string url, string codeName)
    {
        var response = await _client.GetAsync($"{url}?searchString={codeName}");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<PagedResult<ApplicationActionStatusDto>>();
        return body!.Items.First(x => x.CodeName == codeName).Id;
    }

    private async Task<ApplicationActionDto?> CreateActionAsync(Guid applicationId, Guid ResponsibleId,
        string? comment = null)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl,
            new { ApplicationId = applicationId, ResponsibleId = ResponsibleId, Comment = comment });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ApplicationActionDto>();
    }
}
