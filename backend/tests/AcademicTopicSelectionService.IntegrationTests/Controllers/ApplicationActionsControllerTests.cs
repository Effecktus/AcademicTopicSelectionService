using System.Net;
using System.Net.Http.Json;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.Application.ApplicationActions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.ApplicationActionStatuses;
using AcademicTopicSelectionService.Domain.Entities;
using AcademicTopicSelectionService.Infrastructure.Data;
using AcademicTopicSelectionService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AcademicTopicSelectionService.IntegrationTests.Controllers;

[Collection(DatabaseCollection.CollectionName)]
public sealed class ApplicationActionsControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "/api/v1/application-actions";
    private const string ActionStatusesUrl = "/api/v1/application-action-statuses";

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _adminClient;

    public ApplicationActionsControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _adminClient = fixture.CreateAuthenticatedClient(AppRoles.Admin);
    }

    public async Task InitializeAsync() => await _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task List_Returns400_WhenApplicationIdIsEmpty()
    {
        var (_, studentClient, _, _) = await SeedApplicationChainAsync();
        var response = await studentClient.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_Returns200_ForStudentOwner()
    {
        var (ctx, studentClient, _, _) = await SeedApplicationChainAsync();
        var response = await studentClient.GetAsync($"{BaseUrl}?applicationId={ctx.ApplicationId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<ApplicationActionDto>>();
        body!.Total.Should().Be(0);
    }

    [Fact]
    public async Task List_Returns200_ForTeacherOnApplication()
    {
        var (ctx, _, teacherClient, _) = await SeedApplicationChainAsync();
        var response = await teacherClient.GetAsync($"{BaseUrl}?applicationId={ctx.ApplicationId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task List_Returns403_ForUnrelatedUser()
    {
        var (ctx, _, _, otherClient) = await SeedApplicationChainAsync();
        var response = await otherClient.GetAsync($"{BaseUrl}?applicationId={ctx.ApplicationId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_Returns404_WhenApplicationDoesNotExist()
    {
        var (_, studentClient, _, _) = await SeedApplicationChainAsync();
        var response = await studentClient.GetAsync($"{BaseUrl}?applicationId={Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_Returns200_ForFormerResponsible()
    {
        var (ctx, studentClient, _, otherClient) = await SeedApplicationChainAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");
        var createResp = await studentClient.PostAsJsonAsync(BaseUrl,
            new { ApplicationId = ctx.ApplicationId, ResponsibleId = ctx.OtherUserId, Comment = (string?)null });
        createResp.EnsureSuccessStatusCode();

        var listResp = await otherClient.GetAsync($"{BaseUrl}?applicationId={ctx.ApplicationId}");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Returns200_ForParticipant()
    {
        var (ctx, studentClient, _, _) = await SeedApplicationChainAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");
        var created = await CreateActionViaClientAsync(studentClient, ctx.ApplicationId, ctx.StudentUserId);
        var response = await studentClient.GetAsync($"{BaseUrl}/{created!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Returns403_ForUnrelatedUser()
    {
        var (ctx, studentClient, _, otherClient) = await SeedApplicationChainAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");
        var created = await CreateActionViaClientAsync(studentClient, ctx.ApplicationId, ctx.StudentUserId);
        var response = await otherClient.GetAsync($"{BaseUrl}/{created!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_Returns403_ForUnrelatedUser()
    {
        var (ctx, _, _, otherClient) = await SeedApplicationChainAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");
        var response = await otherClient.PostAsJsonAsync(BaseUrl,
            new { ApplicationId = ctx.ApplicationId, ResponsibleId = ctx.TeacherUserId, Comment = (string?)null });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Patch_Returns403_WhenCallerIsNotResponsible()
    {
        var (ctx, studentClient, teacherClient, _) = await SeedApplicationChainAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");
        var approvedId = await CreateActionStatusAsync("Approved", "Согласовано");
        var created = await CreateActionViaClientAsync(studentClient, ctx.ApplicationId, ctx.TeacherUserId);

        var response = await studentClient.PatchAsJsonAsync($"{BaseUrl}/{created!.Id}",
            new { StatusId = approvedId, Comment = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Patch_Returns200_WhenResponsibleUser()
    {
        var (ctx, studentClient, teacherClient, _) = await SeedApplicationChainAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");
        var approvedId = await CreateActionStatusAsync("Approved", "Согласовано");
        var created = await CreateActionViaClientAsync(studentClient, ctx.ApplicationId, ctx.TeacherUserId);

        var response = await teacherClient.PatchAsJsonAsync($"{BaseUrl}/{created!.Id}",
            new { StatusId = approvedId, Comment = (string?)null });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_Returns403_WhenCallerIsNotResponsible()
    {
        var (ctx, studentClient, teacherClient, _) = await SeedApplicationChainAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");
        var created = await CreateActionViaClientAsync(studentClient, ctx.ApplicationId, ctx.TeacherUserId);

        var response = await studentClient.DeleteAsync($"{BaseUrl}/{created!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_Returns204_WhenResponsibleUser()
    {
        var (ctx, studentClient, teacherClient, _) = await SeedApplicationChainAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");
        var created = await CreateActionViaClientAsync(studentClient, ctx.ApplicationId, ctx.TeacherUserId);

        var response = await teacherClient.DeleteAsync($"{BaseUrl}/{created!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_Returns204_WhenAdmin()
    {
        var (ctx, studentClient, _, _) = await SeedApplicationChainAsync();
        await CreateActionStatusAsync("Pending", "На согласовании");
        var created = await CreateActionViaClientAsync(studentClient, ctx.ApplicationId, ctx.StudentUserId);

        var response = await _adminClient.DeleteAsync($"{BaseUrl}/{created!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private sealed record SeedContext(
        Guid ApplicationId,
        Guid StudentUserId,
        Guid TeacherUserId,
        Guid OtherUserId);

    private async Task<(SeedContext ctx, HttpClient studentClient, HttpClient teacherClient, HttpClient otherClient)>
        SeedApplicationChainAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var studentRoleId = Guid.NewGuid();
        var teacherRoleId = Guid.NewGuid();
        db.UserRoles.Add(new UserRole { Id = studentRoleId, CodeName = AppRoles.Student, DisplayName = "Студент" });
        db.UserRoles.Add(new UserRole { Id = teacherRoleId, CodeName = AppRoles.Teacher, DisplayName = "Преподаватель" });

        var studentUserId = Guid.NewGuid();
        var teacherUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        db.Users.Add(new User
        {
            Id = studentUserId,
            Email = $"st_{studentUserId:N}@test.com",
            PasswordHash = "x",
            FirstName = "Студент",
            LastName = "А",
            RoleId = studentRoleId,
            IsActive = true
        });
        db.Users.Add(new User
        {
            Id = teacherUserId,
            Email = $"t_{teacherUserId:N}@test.com",
            PasswordHash = "x",
            FirstName = "Препод",
            LastName = "Б",
            RoleId = teacherRoleId,
            IsActive = true
        });
        db.Users.Add(new User
        {
            Id = otherUserId,
            Email = $"o_{otherUserId:N}@test.com",
            PasswordHash = "x",
            FirstName = "Чужой",
            LastName = "В",
            RoleId = studentRoleId,
            IsActive = true
        });

        var studyGroupId = Guid.NewGuid();
        db.StudyGroups.Add(new StudyGroup { Id = studyGroupId, CodeName = 4401 });

        var studentProfileId = Guid.NewGuid();
        db.Students.Add(new Student { Id = studentProfileId, UserId = studentUserId, GroupId = studyGroupId });

        var otherStudentProfileId = Guid.NewGuid();
        db.Students.Add(new Student { Id = otherStudentProfileId, UserId = otherUserId, GroupId = studyGroupId });

        var topicStatusId = Guid.NewGuid();
        db.TopicStatuses.Add(new TopicStatus { Id = topicStatusId, CodeName = "Active_Test", DisplayName = "Активна" });
        var topicCreatorTypeId = Guid.NewGuid();
        db.TopicCreatorTypes.Add(new TopicCreatorType
            { Id = topicCreatorTypeId, CodeName = "Teacher_Test", DisplayName = "Преподаватель" });

        var topicId = Guid.NewGuid();
        db.Topics.Add(new Topic
        {
            Id = topicId,
            Title = "Тема тест",
            StatusId = topicStatusId,
            CreatorTypeId = topicCreatorTypeId,
            CreatedBy = teacherUserId
        });

        var appStatusId = Guid.NewGuid();
        db.ApplicationStatuses.Add(new ApplicationStatus
            { Id = appStatusId, CodeName = "Pending_App", DisplayName = "Ожидает" });

        var srStatusId = Guid.NewGuid();
        db.ApplicationStatuses.Add(new ApplicationStatus
            { Id = srStatusId, CodeName = "Approved_SR", DisplayName = "Одобрено" });

        var supervisorRequestId = Guid.NewGuid();
        db.SupervisorRequests.Add(new SupervisorRequest
        {
            Id = supervisorRequestId,
            StudentId = studentProfileId,
            TeacherUserId = teacherUserId,
            StatusId = srStatusId
        });

        var applicationId = Guid.NewGuid();
        db.StudentApplications.Add(new StudentApplication
        {
            Id = applicationId,
            StudentId = studentProfileId,
            TopicId = topicId,
            SupervisorRequestId = supervisorRequestId,
            StatusId = appStatusId
        });

        await db.SaveChangesAsync();

        var ctx = new SeedContext(applicationId, studentUserId, teacherUserId, otherUserId);
        var studentClient = _fixture.CreateAuthenticatedClient(AppRoles.Student, studentUserId);
        var teacherClient = _fixture.CreateAuthenticatedClient(AppRoles.Teacher, teacherUserId);
        var otherClient = _fixture.CreateAuthenticatedClient(AppRoles.Student, otherUserId);
        return (ctx, studentClient, teacherClient, otherClient);
    }

    private async Task<Guid> CreateActionStatusAsync(string codeName, string displayName)
    {
        var response = await _adminClient.PostAsJsonAsync(ActionStatusesUrl,
            new { CodeName = codeName, DisplayName = displayName });

        if (!response.IsSuccessStatusCode)
        {
            var list = await _adminClient.GetAsync($"{ActionStatusesUrl}?searchString={codeName}");
            list.EnsureSuccessStatusCode();
            var body = await list.Content.ReadFromJsonAsync<PagedResult<ApplicationActionStatusDto>>();
            return body!.Items.First(x => x.CodeName == codeName).Id;
        }

        var created = await response.Content.ReadFromJsonAsync<ApplicationActionStatusDto>();
        return created!.Id;
    }

    private static async Task<ApplicationActionDto?> CreateActionViaClientAsync(
        HttpClient client,
        Guid applicationId,
        Guid responsibleId)
    {
        var response = await client.PostAsJsonAsync(BaseUrl,
            new { ApplicationId = applicationId, ResponsibleId = responsibleId, Comment = (string?)null });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ApplicationActionDto>();
    }
}
