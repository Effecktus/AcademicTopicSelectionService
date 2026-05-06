using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.Application.ChatMessages;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Notifications;
using AcademicTopicSelectionService.Application.StudentApplications;
using AcademicTopicSelectionService.Application.SupervisorRequests;
using AcademicTopicSelectionService.Application.Topics;
using AcademicTopicSelectionService.Domain.Entities;
using AcademicTopicSelectionService.Infrastructure.Data;
using AcademicTopicSelectionService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AcademicTopicSelectionService.IntegrationTests.Controllers;

[Collection(DatabaseCollection.CollectionName)]
public sealed class ApplicationsIntegrationTests : IAsyncLifetime
{
    private const string AppsBaseUrl = "/api/v1/applications";
    private const string SupervisorRequestsBaseUrl = "/api/v1/supervisor-requests";
    private const string TopicsBaseUrl = "/api/v1/topics";

    private readonly DatabaseFixture _fixture;

    private HttpClient _studentClient = null!;
    private HttpClient _teacherClient = null!;
    private HttpClient _deptHeadClient = null!;

    private Guid _studentUserId;
    private Guid _teacherUserId;
    private Guid _deptHeadUserId;
    private Guid _departmentId;
    private Guid _studentProfileId;
    private Guid _teacherProfileId;

    public ApplicationsIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        await SeedEnvironmentAsync();

        _studentClient = _fixture.CreateAuthenticatedClient(AppRoles.Student, _studentUserId);
        _teacherClient = _fixture.CreateAuthenticatedClient(AppRoles.Teacher, _teacherUserId);
        _deptHeadClient = _fixture.CreateAuthenticatedClient(AppRoles.DepartmentHead, _deptHeadUserId);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // Topics CRUD Integration Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateTopic_Returns201_WhenValid()
    {
        // Sanity: verify the teacher client works at all
        var listResponse = await _teacherClient.GetAsync(TopicsBaseUrl);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK, "teacher should be able to list topics");

        var command = new CreateTopicCommand("Новая тема ВКР", "Описание", "Teacher", "Active");

        var response = await _teacherClient.PostAsJsonAsync(TopicsBaseUrl, command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<TopicDto>();
        body!.Title.Should().Be("Новая тема ВКР");
        body.Description.Should().Be("Описание");
        body.Status.CodeName.Should().Be("Active");
        body.CreatorType.CodeName.Should().Be("Teacher");
        body.CreatedByUserId.Should().Be(_teacherUserId);
    }

    [Fact]
    public async Task CreateTopic_Returns400_WhenTitleEmpty()
    {
        var command = new CreateTopicCommand("", null, "Teacher", null);

        var response = await _teacherClient.PostAsJsonAsync(TopicsBaseUrl, command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTopic_Returns200_WhenAuthor()
    {
        var topicId = await CreateTopicAsync("Старое название");

        // PUT — полная замена, все обязательные поля
        var command = new ReplaceTopicCommand("Новое название", "Новое описание", "Active");
        var response = await _teacherClient.PutAsJsonAsync($"{TopicsBaseUrl}/{topicId}", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TopicDto>();
        body!.Title.Should().Be("Новое название");
        body.Description.Should().Be("Новое описание");
    }

    [Fact]
    public async Task UpdateTopic_Returns403_WhenNotAuthor()
    {
        var topicId = await CreateTopicAsync("Чужая тема");

        // PUT — полная замена, все обязательные поля
        var command = new ReplaceTopicCommand("Хак", null, "Active");
        var response = await _studentClient.PutAsJsonAsync($"{TopicsBaseUrl}/{topicId}", command);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteTopic_Returns204_WhenAuthorNoApplications()
    {
        var topicId = await CreateTopicAsync("Удаляемая тема");

        var response = await _teacherClient.DeleteAsync($"{TopicsBaseUrl}/{topicId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteTopic_Returns400_WhenHasApplications()
    {
        var topicId = await CreateTopicAsync("Тема с заявкой");
        await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);

        var response = await _teacherClient.DeleteAsync($"{TopicsBaseUrl}/{topicId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // -------------------------------------------------------------------------
    // Application Lifecycle Integration Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateApplication_Returns201_WhenValid()
    {
        var topicId = await CreateTopicAsync("Тема для заявки");
        var supervisorRequestId = await CreateApprovedSupervisorRequestAsync(_studentClient, _teacherClient, _teacherUserId);

        var command = new CreateApplicationCommand(topicId, supervisorRequestId);
        var response = await _studentClient.PostAsJsonAsync(AppsBaseUrl, command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<StudentApplicationDto>();
        body!.TopicId.Should().Be(topicId);
        body.Status.CodeName.Should().Be("OnEditing");

        var detail = await GetApplicationAsync(body.Id);
        detail.TopicChangeHistory.Should().HaveCount(2);
        detail.TopicChangeHistory[0].ChangeKind.Should().Be(ApplicationTopicChangeKinds.TopicTitle);
        detail.TopicChangeHistory[0].NewValue.Should().Be("Тема для заявки");
        detail.TopicChangeHistory[0].ChangedByUserId.Should().Be(_studentUserId);
        detail.TopicChangeHistory[1].ChangeKind.Should().Be(ApplicationTopicChangeKinds.TopicDescription);
        detail.TopicChangeHistory[1].NewValue.Should().BeNull();
    }

    [Fact]
    public async Task SubmitToSupervisor_Returns200_WhenOnEditing()
    {
        var topicId = await CreateTopicAsync("Тема для передачи научруку");
        var supervisorRequestId = await CreateApprovedSupervisorRequestAsync(_studentClient, _teacherClient, _teacherUserId);

        var createResponse = await _studentClient.PostAsJsonAsync(
            AppsBaseUrl,
            new CreateApplicationCommand(topicId, supervisorRequestId));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<StudentApplicationDto>();
        created!.Status.CodeName.Should().Be("OnEditing");

        var submitResponse = await _studentClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{created.Id}/submit-to-supervisor",
            new { });
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var after = await submitResponse.Content.ReadFromJsonAsync<StudentApplicationDto>();
        after!.Status.CodeName.Should().Be("Pending");
    }

    [Fact]
    public async Task SubmitToSupervisor_Returns409_WhenAlreadyPending()
    {
        var topicId = await CreateTopicAsync("Повторная передача");
        var supervisorRequestId = await CreateApprovedSupervisorRequestAsync(_studentClient, _teacherClient, _teacherUserId);
        var createResponse = await _studentClient.PostAsJsonAsync(
            AppsBaseUrl,
            new CreateApplicationCommand(topicId, supervisorRequestId));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<StudentApplicationDto>();

        var first = await _studentClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{created!.Id}/submit-to-supervisor",
            new { });
        first.EnsureSuccessStatusCode();

        var second = await _studentClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{created.Id}/submit-to-supervisor",
            new { });
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateTopic_Returns200_WhenOnEditing()
    {
        var topicId = await CreateTopicAsync("Тема для PATCH");
        var supervisorRequestId = await CreateApprovedSupervisorRequestAsync(_studentClient, _teacherClient, _teacherUserId);
        var createResponse = await _studentClient.PostAsJsonAsync(
            AppsBaseUrl,
            new CreateApplicationCommand(topicId, supervisorRequestId));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<StudentApplicationDto>();

        var patchResponse = await _studentClient.PatchAsJsonAsync(
            $"{AppsBaseUrl}/{created!.Id}/topic",
            new UpdateApplicationTopicCommand("Обновлённое название", "Обновлённое описание"));
        patchResponse.EnsureSuccessStatusCode();
        var body = await patchResponse.Content.ReadFromJsonAsync<StudentApplicationDto>();
        body!.TopicTitle.Should().Be("Обновлённое название");

        var detail = await GetApplicationAsync(created.Id);
        detail.TopicChangeHistory.Should().HaveCount(4);
        detail.TopicChangeHistory[0].ChangeKind.Should().Be(ApplicationTopicChangeKinds.TopicTitle);
        detail.TopicChangeHistory[0].NewValue.Should().Be("Тема для PATCH");
        detail.TopicChangeHistory[0].ChangedByUserId.Should().Be(_studentUserId);
        detail.TopicChangeHistory[1].ChangeKind.Should().Be(ApplicationTopicChangeKinds.TopicDescription);
        detail.TopicChangeHistory[1].NewValue.Should().BeNull();
        detail.TopicChangeHistory[2].ChangeKind.Should().Be(ApplicationTopicChangeKinds.TopicTitle);
        detail.TopicChangeHistory[2].NewValue.Should().Be("Обновлённое название");
        detail.TopicChangeHistory[2].ChangedByUserId.Should().Be(_studentUserId);
        detail.TopicChangeHistory[3].ChangeKind.Should().Be(ApplicationTopicChangeKinds.TopicDescription);
        detail.TopicChangeHistory[3].NewValue.Should().Be("Обновлённое описание");
    }

    [Fact]
    public async Task UpdateTopic_DoesNotAddHistory_WhenValuesAreSameAfterTrim()
    {
        var topicId = await CreateTopicAsync("Тема без изменений");
        var supervisorRequestId = await CreateApprovedSupervisorRequestAsync(_studentClient, _teacherClient, _teacherUserId);
        var createResponse = await _studentClient.PostAsJsonAsync(
            AppsBaseUrl,
            new CreateApplicationCommand(topicId, supervisorRequestId));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<StudentApplicationDto>();

        var patchResponse = await _studentClient.PatchAsJsonAsync(
            $"{AppsBaseUrl}/{created!.Id}/topic",
            new UpdateApplicationTopicCommand("  Тема без изменений  ", null));
        patchResponse.EnsureSuccessStatusCode();

        var detail = await GetApplicationAsync(created.Id);
        detail.TopicChangeHistory.Should().HaveCount(2, "initial title/description records should stay unchanged");
    }

    [Fact]
    public async Task ReturnForEditingBySupervisor_Returns200_WhenPending()
    {
        var topicId = await CreateTopicAsync("Возврат научруком");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);

        var response = await _teacherClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/return-for-editing",
            new ReturnApplicationForEditingCommand("Уточните формулировку цели работы"));

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<StudentApplicationDto>();
        dto!.Status.CodeName.Should().Be("OnEditing");
    }

    [Fact]
    public async Task ReturnForEditingBySupervisor_Returns400_WhenCommentIsWhitespace()
    {
        var topicId = await CreateTopicAsync("Возврат без комментария");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);

        var response = await _teacherClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/return-for-editing",
            new ReturnApplicationForEditingCommand("   "));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DepartmentHead_ReturnForEditing_ThenStudentResubmits_AndSupervisorApprovesAgain()
    {
        var topicId = await CreateTopicAsync("Цикл возврата завкафом");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);
        await _teacherClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/approve", new ApproveBySupervisorCommand(null));

        var returnResponse = await _deptHeadClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/department-head-return-for-editing",
            new ReturnApplicationForEditingCommand("Доработайте раздел актуальности"));
        returnResponse.EnsureSuccessStatusCode();
        var returned = await returnResponse.Content.ReadFromJsonAsync<StudentApplicationDto>();
        returned!.Status.CodeName.Should().Be("OnEditing");

        var submitResponse = await _studentClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/submit-to-supervisor",
            new { });
        submitResponse.EnsureSuccessStatusCode();

        var approveResponse = await _teacherClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/approve", new ApproveBySupervisorCommand("Принято после доработки"));
        approveResponse.EnsureSuccessStatusCode();
        var final = await approveResponse.Content.ReadFromJsonAsync<StudentApplicationDto>();
        final!.Status.CodeName.Should().Be("PendingDepartmentHead");
    }

    [Fact]
    public async Task DepartmentHeadReturnForEditing_Returns400_WhenCommentIsWhitespace()
    {
        var topicId = await CreateTopicAsync("Возврат завкаф без комментария");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);
        await _teacherClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/approve", new ApproveBySupervisorCommand(null));

        var response = await _deptHeadClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/department-head-return-for-editing",
            new ReturnApplicationForEditingCommand("   "));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cancel_Returns204_FromOnEditing()
    {
        var topicId = await CreateTopicAsync("Отмена в черновике");
        var supervisorRequestId = await CreateApprovedSupervisorRequestAsync(_studentClient, _teacherClient, _teacherUserId);
        var createResponse = await _studentClient.PostAsJsonAsync(
            AppsBaseUrl,
            new CreateApplicationCommand(topicId, supervisorRequestId));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<StudentApplicationDto>();
        created!.Status.CodeName.Should().Be("OnEditing");

        var response = await _studentClient.PutAsync($"{AppsBaseUrl}/{created.Id}/cancel", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateApplication_Returns201_WhenStudentProposesTopicInSingleRequest()
    {
        var supervisorRequestId = await CreateApprovedSupervisorRequestAsync(_studentClient, _teacherClient, _teacherUserId);
        var proposedTitle = "Тема от студента одним запросом";

        var command = new CreateApplicationCommand(null, supervisorRequestId, proposedTitle, "Описание темы");
        var response = await _studentClient.PostAsJsonAsync(AppsBaseUrl, command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<StudentApplicationDto>();
        body.Should().NotBeNull();
        body!.TopicTitle.Should().Be(proposedTitle);
        body.TopicCreatedByUserId.Should().Be(_studentUserId);

        var detail = await GetApplicationAsync(body.Id);
        detail.TopicChangeHistory.Should().HaveCount(2);
        detail.TopicChangeHistory[0].ChangeKind.Should().Be(ApplicationTopicChangeKinds.TopicTitle);
        detail.TopicChangeHistory[0].NewValue.Should().Be(proposedTitle);
        detail.TopicChangeHistory[1].ChangeKind.Should().Be(ApplicationTopicChangeKinds.TopicDescription);
        detail.TopicChangeHistory[1].NewValue.Should().Be("Описание темы");
    }

    [Fact]
    public async Task CreateApplication_Returns400_WhenNeitherTopicIdNorProposedTitleProvided()
    {
        var supervisorRequestId = await CreateApprovedSupervisorRequestAsync(_studentClient, _teacherClient, _teacherUserId);

        var response = await _studentClient.PostAsJsonAsync(
            AppsBaseUrl,
            new CreateApplicationCommand(null, supervisorRequestId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateApplication_Returns400_WhenBothTopicIdAndProposedTitleProvided()
    {
        var topicId = await CreateTopicAsync("Тема для mixed payload");
        var supervisorRequestId = await CreateApprovedSupervisorRequestAsync(_studentClient, _teacherClient, _teacherUserId);

        var response = await _studentClient.PostAsJsonAsync(
            AppsBaseUrl,
            new CreateApplicationCommand(topicId, supervisorRequestId, "Лишний title", "Лишний description"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateApplication_Returns400_WhenSupervisorRequestIdIsEmpty()
    {
        var topicId = await CreateTopicAsync("Тема без supervisor request");

        var response = await _studentClient.PostAsJsonAsync(
            AppsBaseUrl,
            new CreateApplicationCommand(topicId, Guid.Empty));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateApplication_Returns404_WhenTopicNotFound()
    {
        var supervisorRequestId = await CreateApprovedSupervisorRequestAsync(_studentClient, _teacherClient, _teacherUserId);

        var response = await _studentClient.PostAsJsonAsync(
            AppsBaseUrl,
            new CreateApplicationCommand(Guid.NewGuid(), supervisorRequestId));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateApplication_Returns400_WhenTopicInactive()
    {
        var topicId = await CreateTopicAsync("Неактивная тема", statusCodeName: "Inactive");
        var supervisorRequestId = await CreateApprovedSupervisorRequestAsync(_studentClient, _teacherClient, _teacherUserId);

        var response = await _studentClient.PostAsJsonAsync(
            AppsBaseUrl,
            new CreateApplicationCommand(topicId, supervisorRequestId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateApplication_Returns400_WhenProposedTitleIsWhitespace()
    {
        var supervisorRequestId = await CreateApprovedSupervisorRequestAsync(_studentClient, _teacherClient, _teacherUserId);

        var response = await _studentClient.PostAsJsonAsync(
            AppsBaseUrl,
            new CreateApplicationCommand(null, supervisorRequestId, "   ", "Описание"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateApplication_Returns400_WhenProposedTitleTooLong()
    {
        var supervisorRequestId = await CreateApprovedSupervisorRequestAsync(_studentClient, _teacherClient, _teacherUserId);
        var tooLongTitle = new string('А', 501);

        var response = await _studentClient.PostAsJsonAsync(
            AppsBaseUrl,
            new CreateApplicationCommand(null, supervisorRequestId, tooLongTitle, "Описание"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateApplication_Returns400_WhenSupervisorRequestBelongsToAnotherStudent()
    {
        var topicId = await CreateTopicAsync("Чужой supervisor request");
        var student2Id = await CreateStudentUserAsync("student-foreign-request@test.com");
        var student2Client = _fixture.CreateAuthenticatedClient(AppRoles.Student, student2Id);
        var student2SupervisorRequestId = await CreateApprovedSupervisorRequestAsync(student2Client, _teacherClient, _teacherUserId);

        var response = await _studentClient.PostAsJsonAsync(
            AppsBaseUrl,
            new CreateApplicationCommand(topicId, student2SupervisorRequestId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateApplication_Returns400_WhenSupervisorRequestIsNotApproved()
    {
        var topicId = await CreateTopicAsync("Неодобренный supervisor request");
        var pendingSupervisorRequestId = await CreatePendingSupervisorRequestAsync(_studentClient, _teacherUserId);

        var response = await _studentClient.PostAsJsonAsync(
            AppsBaseUrl,
            new CreateApplicationCommand(topicId, pendingSupervisorRequestId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateApplication_Returns400_WhenTopicDoesNotBelongToApprovedSupervisor()
    {
        var otherTeacherId = await CreateTeacherUserAsync("topic-owner@test.com", _departmentId);
        var supervisorRequestId = await CreateApprovedSupervisorRequestAsync(_studentClient, _teacherClient, _teacherUserId);

        var otherTeacherClient = _fixture.CreateAuthenticatedClient(AppRoles.Teacher, otherTeacherId);
        var createTopicResponse = await otherTeacherClient.PostAsJsonAsync(
            TopicsBaseUrl,
            new CreateTopicCommand("Тема другого научрука", null, "Teacher", "Active"));
        createTopicResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var topic = await createTopicResponse.Content.ReadFromJsonAsync<TopicDto>();
        topic.Should().NotBeNull();

        var response = await _studentClient.PostAsJsonAsync(
            AppsBaseUrl,
            new CreateApplicationCommand(topic!.Id, supervisorRequestId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateApplication_Returns400_WhenStudentAlreadyHasActiveApplication()
    {
        var topicId1 = await CreateTopicAsync("Первая тема");
        var topicId2 = await CreateTopicAsync("Вторая тема");
        var supervisorRequestId = await CreateApprovedSupervisorRequestAsync(_studentClient, _teacherClient, _teacherUserId);
        var first = await _studentClient.PostAsJsonAsync(
            AppsBaseUrl,
            new CreateApplicationCommand(topicId1, supervisorRequestId));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await _studentClient.PostAsJsonAsync(
            AppsBaseUrl,
            new CreateApplicationCommand(topicId2, supervisorRequestId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateApplication_Returns403_WhenUserIsTeacher()
    {
        var topicId = await CreateTopicAsync("Тема для заявки учителя");
        var supervisorRequestId = await CreateApprovedSupervisorRequestAsync(_studentClient, _teacherClient, _teacherUserId);

        // Преподаватель не может подавать заявки
        var response = await _teacherClient.PostAsJsonAsync(AppsBaseUrl, new CreateApplicationCommand(topicId, supervisorRequestId));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateApplication_Returns409_WhenTopicAlreadyTaken()
    {
        var topicId = await CreateTopicAsync("Конкурентная тема");
        await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);

        var student2Id = await CreateStudentUserAsync("student2@test.com");
        var student2Client = _fixture.CreateAuthenticatedClient(AppRoles.Student, student2Id);

        var student2SupervisorRequestId = await CreateApprovedSupervisorRequestAsync(student2Client, _teacherClient, _teacherUserId);
        var response = await student2Client.PostAsJsonAsync(AppsBaseUrl, new CreateApplicationCommand(topicId, student2SupervisorRequestId));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ApproveBySupervisor_Returns200_WhenPending()
    {
        var topicId = await CreateTopicAsync("Тема для одобрения");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);

        var response = await _teacherClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/approve", new ApproveBySupervisorCommand("Хорошая заявка"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<StudentApplicationDto>();
        body!.Status.CodeName.Should().Be("PendingDepartmentHead");
    }

    [Fact]
    public async Task ApproveBySupervisor_Returns403_WhenNotSupervisor()
    {
        var topicId = await CreateTopicAsync("Чужая тема");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);

        var otherTeacherId = await CreateTeacherUserAsync("other-teach@test.com", _departmentId);
        var otherTeacherClient = _fixture.CreateAuthenticatedClient(AppRoles.Teacher, otherTeacherId);

        var response = await otherTeacherClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/approve", new ApproveBySupervisorCommand(null));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RejectBySupervisor_Returns400_WhenCommentEmpty()
    {
        var topicId = await CreateTopicAsync("Тема для отклонения");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);

        var response = await _teacherClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/reject", new RejectBySupervisorCommand(""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ApproveByDepartmentHead_Returns200_WhenPendingDepartmentHead()
    {
        var topicId = await CreateTopicAsync("Тема для зав. кафедрой");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);
        await _teacherClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/approve", new ApproveBySupervisorCommand(null));

        var response = await _deptHeadClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/department-head-approve", new ApproveByDepartmentHeadCommand("Утверждено"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<StudentApplicationDto>();
        body!.Status.CodeName.Should().Be("ApprovedByDepartmentHead");
    }

    [Fact]
    public async Task Cancel_Returns204_FromPending()
    {
        var topicId = await CreateTopicAsync("Тема для отмены");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);

        var response = await _studentClient.PutAsync($"{AppsBaseUrl}/{appId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Cancel_Returns403_WhenStudentTriesToCancelForeignApplication()
    {
        var topicId = await CreateTopicAsync("Чужая заявка для отмены");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);
        var otherStudentUserId = await CreateStudentUserAsync("cancel-foreign@test.com");
        var otherStudentClient = _fixture.CreateAuthenticatedClient(AppRoles.Student, otherStudentUserId);

        var response = await otherStudentClient.PutAsync($"{AppsBaseUrl}/{appId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cancel_Returns409_AfterSupervisorApprove_BecauseApplicationIsAtDepartmentHead()
    {
        var topicId = await CreateTopicAsync("Тема для отмены после одобрения");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);
        await _teacherClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/approve", new ApproveBySupervisorCommand(null));

        var response = await _studentClient.PutAsync($"{AppsBaseUrl}/{appId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cancel_Returns409_FromPendingDepartmentHead()
    {
        var topicId = await CreateTopicAsync("Тема нельзя отменить");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);
        await _teacherClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/approve", new ApproveBySupervisorCommand(null));

        var response = await _studentClient.PutAsync($"{AppsBaseUrl}/{appId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task FullLifecycle_CompletesSuccessfully()
    {
        var topicId = await CreateTopicAsync("Полный цикл");

        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);
        var app = await GetApplicationAsync(appId);
        app.Status.CodeName.Should().Be("Pending");

        await _teacherClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/approve", new ApproveBySupervisorCommand("Одобрено"));
        app = await GetApplicationAsync(appId);
        app.Status.CodeName.Should().Be("PendingDepartmentHead");

        await _deptHeadClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/department-head-approve", new ApproveByDepartmentHeadCommand("Утверждено"));
        app = await GetApplicationAsync(appId);
        app.Status.CodeName.Should().Be("ApprovedByDepartmentHead");

        var cancelResponse = await _studentClient.PutAsync($"{AppsBaseUrl}/{appId}/cancel", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ListForStudent_ReturnsOnlyOwnApplications()
    {
        var topicId = await CreateTopicAsync("Тема для списка");
        await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);

        var response = await _studentClient.GetAsync(AppsBaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<StudentApplicationDto>>();
        body!.Total.Should().Be(1);
    }

    [Fact]
    public async Task GetDetail_ReturnsApplicationWithActions()
    {
        var topicId = await CreateTopicAsync("Тема с историей");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);
        await _teacherClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/approve", new ApproveBySupervisorCommand("Одобрено"));

        var response = await _teacherClient.GetAsync($"{AppsBaseUrl}/{appId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<StudentApplicationDetailDto>();
        body!.Id.Should().Be(appId);
        body.Actions.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetDetail_Returns404_WhenStudentRequestsAnotherStudentsApplication()
    {
        var topicId = await CreateTopicAsync("Чужая заявка — GET по id");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);
        var otherStudentUserId = await CreateStudentUserAsync("other-get-detail@test.com");
        var otherStudentClient = _fixture.CreateAuthenticatedClient(AppRoles.Student, otherStudentUserId);

        var response = await otherStudentClient.GetAsync($"{AppsBaseUrl}/{appId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // Chat (polling)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Chat_StudentPostsTeacherMarksIncomingRead()
    {
        var topicId = await CreateTopicAsync("Тема для чата");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);
        var msgUrl = $"{AppsBaseUrl}/{appId}/messages";

        var post = await _studentClient.PostAsJsonAsync(msgUrl, new { content = "Здравствуйте" });
        post.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await post.Content.ReadFromJsonAsync<ChatMessageDto>();
        created!.Content.Should().Be("Здравствуйте");
        created.SenderId.Should().Be(_studentUserId);

        var listTeacher = await _teacherClient.GetAsync(msgUrl);
        listTeacher.EnsureSuccessStatusCode();
        var messages = await listTeacher.Content.ReadFromJsonAsync<ChatMessageDto[]>();
        messages!.Should().ContainSingle(m => m.Id == created.Id);

        var readPut = await _teacherClient.PutAsync($"{msgUrl}/read-all", null);
        readPut.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var row = await db.ChatMessages.AsNoTracking().FirstAsync(m => m.Id == created.Id);
        row.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Chat_StudentPost_CreatesNewMessageNotificationForTeacher()
    {
        var topicId = await CreateTopicAsync("Чат: уведомление преподавателю");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);
        var msgUrl = $"{AppsBaseUrl}/{appId}/messages";

        var post = await _studentClient.PostAsJsonAsync(msgUrl, new { content = "Нужна консультация" });
        post.StatusCode.Should().Be(HttpStatusCode.Created);

        var list = await _teacherClient.GetAsync("/api/v1/notifications?isRead=false&page=1&pageSize=50");
        list.EnsureSuccessStatusCode();
        var body = await list.Content.ReadFromJsonAsync<PagedResult<NotificationDto>>();
        body!.Items.Should().Contain(n =>
            n.TypeCodeName == "NewMessage" &&
            n.Content.Contains("Нужна консультация"));
    }

    [Fact]
    public async Task Chat_DepartmentHead_GetMessages_Returns403()
    {
        var topicId = await CreateTopicAsync("Чат без доступа завкаф");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);
        var response = await _deptHeadClient.GetAsync($"{AppsBaseUrl}/{appId}/messages");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Chat_OtherStudent_PostReturns403()
    {
        var topicId = await CreateTopicAsync("Чужой чат");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);
        var otherStudentUserId = await CreateStudentUserAsync("other-chat@test.com");
        var otherClient = _fixture.CreateAuthenticatedClient(AppRoles.Student, otherStudentUserId);

        var response = await otherClient.PostAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/messages", new { content = "Взлом" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Chat_GetMessages_Returns404_WhenApplicationNotExists()
    {
        var missingId = Guid.NewGuid();
        var response = await _studentClient.GetAsync($"{AppsBaseUrl}/{missingId}/messages");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Chat_Post_Returns400_WhenContentWhitespace()
    {
        var topicId = await CreateTopicAsync("Чат валидация пробелы");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);
        var response = await _studentClient.PostAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/messages", new { content = "   \t" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Chat_Post_Returns400_WhenContentTooLong()
    {
        var topicId = await CreateTopicAsync("Чат валидация длина");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);
        var response = await _studentClient.PostAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/messages", new { content = new string('x', 4001) });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Chat_GetMessages_WithAfterId_ReturnsOnlyNewerMessages()
    {
        var topicId = await CreateTopicAsync("Чат курсор afterId");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);
        var msgUrl = $"{AppsBaseUrl}/{appId}/messages";

        var r1 = await _studentClient.PostAsJsonAsync(msgUrl, new { content = "first" });
        r1.EnsureSuccessStatusCode();
        var m1 = await r1.Content.ReadFromJsonAsync<ChatMessageDto>();

        await Task.Delay(50);

        var r2 = await _studentClient.PostAsJsonAsync(msgUrl, new { content = "second" });
        r2.EnsureSuccessStatusCode();
        var m2 = await r2.Content.ReadFromJsonAsync<ChatMessageDto>();

        var list = await _studentClient.GetAsync($"{msgUrl}?afterId={m1!.Id}");
        list.EnsureSuccessStatusCode();
        var arr = await list.Content.ReadFromJsonAsync<ChatMessageDto[]>();
        arr.Should().NotBeNull();
        arr!.Should().Contain(m => m.Id == m2!.Id);
        arr.Should().NotContain(m => m.Id == m1.Id);
    }

    [Fact]
    public async Task Chat_ReadAll_Returns404_WhenApplicationNotExists()
    {
        var missingId = Guid.NewGuid();
        var response = await _teacherClient.PutAsync($"{AppsBaseUrl}/{missingId}/messages/read-all", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Chat_ReadAll_Returns403_ForDepartmentHead()
    {
        var topicId = await CreateTopicAsync("Чат read-all завкаф");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);
        var response = await _deptHeadClient.PutAsync($"{AppsBaseUrl}/{appId}/messages/read-all", null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Chat_AfterSupervisorRejectsApplication_StudentCanListMessages_ButCannotPost()
    {
        var topicId = await CreateTopicAsync("Чат после отклонения заявки");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);
        var msgUrl = $"{AppsBaseUrl}/{appId}/messages";

        var postBefore = await _studentClient.PostAsJsonAsync(msgUrl, new { content = "До отклонения" });
        postBefore.StatusCode.Should().Be(HttpStatusCode.Created);

        var reject = await _teacherClient.PutAsJsonAsync(
            $"{AppsBaseUrl}/{appId}/reject", new RejectBySupervisorCommand("Не подходит"));
        reject.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await _studentClient.GetAsync(msgUrl);
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var arr = await list.Content.ReadFromJsonAsync<ChatMessageDto[]>();
        arr.Should().NotBeNull();
        arr!.Should().Contain(m => m.Content == "До отклонения");

        var postAfter = await _studentClient.PostAsJsonAsync(msgUrl, new { content = "После отклонения" });
        postAfter.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Chat_TeacherReply_SetsReadAtOnStudentMessages()
    {
        var topicId = await CreateTopicAsync("Чат readAt после ответа препода");
        var appId = await CreateApplicationAsync(_studentClient, _teacherClient, _teacherUserId, topicId, HttpStatusCode.Created);
        var msgUrl = $"{AppsBaseUrl}/{appId}/messages";

        var studentPost = await _studentClient.PostAsJsonAsync(msgUrl, new { content = "Вопрос студента" });
        studentPost.EnsureSuccessStatusCode();
        var studentMsg = await studentPost.Content.ReadFromJsonAsync<ChatMessageDto>();

        var teacherReply = await _teacherClient.PostAsJsonAsync(msgUrl, new { content = "Ответ" });
        teacherReply.StatusCode.Should().Be(HttpStatusCode.Created);

        var list = await _studentClient.GetAsync(msgUrl);
        list.EnsureSuccessStatusCode();
        var messages = await list.Content.ReadFromJsonAsync<ChatMessageDto[]>();
        messages!.Should().ContainSingle(m => m.Id == studentMsg!.Id).Which.ReadAt.Should().NotBeNull();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task SeedEnvironmentAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _departmentId = Guid.NewGuid();
        db.Departments.Add(new Department
        {
            Id = _departmentId,
            CodeName = "test_department",
            DisplayName = "Тестовая кафедра"
        });

        // Создаём роли с правильными CodeName — сервисы проверяют role.CodeName
        var studentRoleId = Guid.NewGuid();
        var teacherRoleId = Guid.NewGuid();
        var deptHeadRoleId = Guid.NewGuid();
        db.UserRoles.Add(new UserRole { Id = studentRoleId, CodeName = AppRoles.Student, DisplayName = "Студент" });
        db.UserRoles.Add(new UserRole { Id = teacherRoleId, CodeName = AppRoles.Teacher, DisplayName = "Преподаватель" });
        db.UserRoles.Add(new UserRole { Id = deptHeadRoleId, CodeName = AppRoles.DepartmentHead, DisplayName = "Зав. кафедрой" });
        db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), CodeName = "TestRole", DisplayName = "Тестовая роль" });

        _studentUserId = Guid.NewGuid();
        _studentProfileId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = _studentUserId,
            Email = "student@test.com",
            PasswordHash = "x",
            FirstName = "Студент",
            LastName = "Тестов",
            RoleId = studentRoleId,
            IsActive = true,
            DepartmentId = _departmentId
        });
        db.Students.Add(new Student
        {
            Id = _studentProfileId,
            UserId = _studentUserId,
            GroupId = await EnsureStudyGroupAsync(db, 4411)
        });

        _teacherUserId = Guid.NewGuid();
        _teacherProfileId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = _teacherUserId,
            Email = "teacher@test.com",
            PasswordHash = "x",
            FirstName = "Преподаватель",
            LastName = "Тестов",
            RoleId = teacherRoleId,
            IsActive = true,
            DepartmentId = _departmentId
        });
        db.Teachers.Add(new Teacher
        {
            Id = _teacherProfileId,
            UserId = _teacherUserId,
            MaxStudentsLimit = 10,
            AcademicDegreeId = await EnsureAcademicDegreeAsync(db, "None"),
            AcademicTitleId = await EnsureAcademicTitleAsync(db, "None"),
            PositionId = await EnsurePositionAsync(db, "Assistant")
        });

        _deptHeadUserId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = _deptHeadUserId,
            Email = "depthead@test.com",
            PasswordHash = "x",
            FirstName = "Зав",
            LastName = "Кафедрой",
            RoleId = deptHeadRoleId,
            IsActive = true,
            DepartmentId = _departmentId
        });

        var dept = await db.Departments.FindAsync(_departmentId);
        if (dept is not null)
            dept.HeadId = _deptHeadUserId;

        await EnsureApplicationStatusesAsync(db);
        await EnsureTopicStatusesAsync(db);
        await EnsureTopicCreatorTypesAsync(db);
        await EnsureApplicationActionStatusesAsync(db);
        await NotificationTypesTestSeed.EnsureAsync(db);

        await db.SaveChangesAsync();
    }

    private async Task<Guid> CreateStudentUserAsync(string email)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var role = await db.UserRoles.FirstAsync(r => r.CodeName == AppRoles.Student);
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = email,
            PasswordHash = "x",
            FirstName = "Another",
            LastName = "Student",
            RoleId = role.Id,
            IsActive = true,
            DepartmentId = _departmentId
        });
        db.Students.Add(new Student
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GroupId = await EnsureStudyGroupAsync(db, 4412)
        });
        await db.SaveChangesAsync();
        return userId;
    }

    private async Task<Guid> CreateTeacherUserAsync(string email, Guid departmentId)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var role = await db.UserRoles.FirstAsync(r => r.CodeName == AppRoles.Teacher);
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = email,
            PasswordHash = "x",
            FirstName = "Other",
            LastName = "Teacher",
            RoleId = role.Id,
            IsActive = true,
            DepartmentId = departmentId
        });
        db.Teachers.Add(new Teacher
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MaxStudentsLimit = 5,
            AcademicDegreeId = await EnsureAcademicDegreeAsync(db, "None"),
            AcademicTitleId = await EnsureAcademicTitleAsync(db, "None"),
            PositionId = await EnsurePositionAsync(db, "Assistant")
        });
        await db.SaveChangesAsync();
        return userId;
    }

    private async Task<Guid> CreateTopicAsync(string title, string statusCodeName = "Active")
    {
        var response = await _teacherClient.PostAsJsonAsync(TopicsBaseUrl,
            new CreateTopicCommand(title, null, "Teacher", statusCodeName));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TopicDto>();
        return body!.Id;
    }

    private async Task<Guid> CreateApplicationAsync(
        HttpClient studentClient,
        HttpClient teacherClient,
        Guid teacherUserId,
        Guid topicId,
        HttpStatusCode expectedStatus)
    {
        var supervisorRequestId = await CreateApprovedSupervisorRequestAsync(studentClient, teacherClient, teacherUserId);
        var response = await studentClient.PostAsJsonAsync(AppsBaseUrl, new CreateApplicationCommand(topicId, supervisorRequestId));
        response.StatusCode.Should().Be(expectedStatus);
        var body = await response.Content.ReadFromJsonAsync<StudentApplicationDto>();
        var appId = body!.Id;
        if (expectedStatus != HttpStatusCode.Created)
            return appId;

        var submitResponse = await studentClient.PutAsJsonAsync($"{AppsBaseUrl}/{appId}/submit-to-supervisor", new { });
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return appId;
    }

    private static async Task<Guid> CreateApprovedSupervisorRequestAsync(
        HttpClient studentClient,
        HttpClient teacherClient,
        Guid teacherUserId)
    {
        var createResponse = await studentClient.PostAsJsonAsync(
            SupervisorRequestsBaseUrl,
            new CreateSupervisorRequestCommand(teacherUserId, "Прошу одобрить"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<SupervisorRequestDto>();

        var approveResponse = await teacherClient.PutAsync($"{SupervisorRequestsBaseUrl}/{created!.Id}/approve", null);
        approveResponse.EnsureSuccessStatusCode();

        return created.Id;
    }

    private static async Task<Guid> CreatePendingSupervisorRequestAsync(
        HttpClient studentClient,
        Guid teacherUserId)
    {
        var createResponse = await studentClient.PostAsJsonAsync(
            SupervisorRequestsBaseUrl,
            new CreateSupervisorRequestCommand(teacherUserId, "Прошу одобрить"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<SupervisorRequestDto>();
        return created!.Id;
    }

    private async Task<StudentApplicationDetailDto> GetApplicationAsync(Guid appId)
    {
        var response = await _teacherClient.GetAsync($"{AppsBaseUrl}/{appId}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StudentApplicationDetailDto>())!;
    }

    private static async Task<Guid> EnsureStudyGroupAsync(ApplicationDbContext db, int codeName)
    {
        var existing = await db.StudyGroups.FirstOrDefaultAsync(g => g.CodeName == codeName);
        if (existing is not null) return existing.Id;
        var id = Guid.NewGuid();
        db.StudyGroups.Add(new StudyGroup { Id = id, CodeName = codeName });
        await db.SaveChangesAsync();
        return id;
    }

    private static async Task<Guid> EnsureAcademicDegreeAsync(ApplicationDbContext db, string codeName)
    {
        var existing = await db.AcademicDegrees.FirstOrDefaultAsync(d => d.CodeName == codeName);
        if (existing is not null) return existing.Id;
        var id = Guid.NewGuid();
        db.AcademicDegrees.Add(new AcademicDegree { Id = id, CodeName = codeName, DisplayName = codeName });
        await db.SaveChangesAsync();
        return id;
    }

    private static async Task<Guid> EnsureAcademicTitleAsync(ApplicationDbContext db, string codeName)
    {
        var existing = await db.AcademicTitles.FirstOrDefaultAsync(t => t.CodeName == codeName);
        if (existing is not null) return existing.Id;
        var id = Guid.NewGuid();
        db.AcademicTitles.Add(new AcademicTitle { Id = id, CodeName = codeName, DisplayName = codeName });
        await db.SaveChangesAsync();
        return id;
    }

    private static async Task<Guid> EnsurePositionAsync(ApplicationDbContext db, string codeName)
    {
        var existing = await db.Positions.FirstOrDefaultAsync(p => p.CodeName == codeName);
        if (existing is not null) return existing.Id;
        var id = Guid.NewGuid();
        db.Positions.Add(new Position { Id = id, CodeName = codeName, DisplayName = codeName });
        await db.SaveChangesAsync();
        return id;
    }

    private static async Task EnsureApplicationStatusesAsync(ApplicationDbContext db)
    {
        var statuses = new[]
        {
            ("OnEditing", "На редактировании"),
            ("Pending", "Ожидает"),
            ("ApprovedBySupervisor", "Одобрено преподавателем"),
            ("RejectedBySupervisor", "Отклонено преподавателем"),
            ("PendingDepartmentHead", "Отправлено заведующему"),
            ("ApprovedByDepartmentHead", "Утверждено"),
            ("RejectedByDepartmentHead", "Отклонено зав."),
            ("Cancelled", "Отменено")
        };
        foreach (var (code, display) in statuses)
        {
            if (!await db.ApplicationStatuses.AnyAsync(s => s.CodeName == code))
                db.ApplicationStatuses.Add(new ApplicationStatus { Id = Guid.NewGuid(), CodeName = code, DisplayName = display });
        }
    }

    private static async Task EnsureTopicStatusesAsync(ApplicationDbContext db)
    {
        foreach (var code in new[] { "Active", "Inactive" })
        {
            if (!await db.TopicStatuses.AnyAsync(s => s.CodeName == code))
                db.TopicStatuses.Add(new TopicStatus { Id = Guid.NewGuid(), CodeName = code, DisplayName = code });
        }
    }

    private static async Task EnsureTopicCreatorTypesAsync(ApplicationDbContext db)
    {
        foreach (var (code, display) in new[] { ("Teacher", "Научный руководитель"), ("Student", "Студент") })
        {
            if (!await db.TopicCreatorTypes.AnyAsync(s => s.CodeName == code))
                db.TopicCreatorTypes.Add(new TopicCreatorType { Id = Guid.NewGuid(), CodeName = code, DisplayName = display });
        }
    }

    private static async Task EnsureApplicationActionStatusesAsync(ApplicationDbContext db)
    {
        foreach (var (code, display) in new[] { ("Pending", "На согласовании"), ("Approved", "Согласовано"), ("Rejected", "Отклонено"), ("ReturnedForEditing", "Возвращено на редактирование"), ("Cancelled", "Отменено") })
        {
            if (!await db.ApplicationActionStatuses.AnyAsync(s => s.CodeName == code))
                db.ApplicationActionStatuses.Add(new ApplicationActionStatus { Id = Guid.NewGuid(), CodeName = code, DisplayName = display });
        }
    }
}
