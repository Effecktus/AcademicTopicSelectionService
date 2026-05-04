using System.Net;
using System.Net.Http.Json;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.SupervisorRequests;
using AcademicTopicSelectionService.Domain.Entities;
using AcademicTopicSelectionService.Infrastructure.Data;
using AcademicTopicSelectionService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AcademicTopicSelectionService.IntegrationTests.Controllers;

[Collection(DatabaseCollection.CollectionName)]
public sealed class SupervisorRequestsIntegrationTests : IAsyncLifetime
{
    private const string BaseUrl = "/api/v1/supervisor-requests";

    private readonly DatabaseFixture _fixture;

    private HttpClient _studentClient = null!;
    private HttpClient _student2Client = null!;
    private HttpClient _teacherClient = null!;
    private HttpClient _teacher2Client = null!;
    private HttpClient _adminClient = null!;

    private Guid _studentUserId;
    private Guid _student2UserId;
    private Guid _teacherUserId;
    private Guid _teacher2UserId;
    private Guid _adminUserId;

    public SupervisorRequestsIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        await SeedEnvironmentAsync();

        _studentClient = _fixture.CreateAuthenticatedClient(AppRoles.Student, _studentUserId);
        _student2Client = _fixture.CreateAuthenticatedClient(AppRoles.Student, _student2UserId);
        _teacherClient = _fixture.CreateAuthenticatedClient(AppRoles.Teacher, _teacherUserId);
        _teacher2Client = _fixture.CreateAuthenticatedClient(AppRoles.Teacher, _teacher2UserId);
        _adminClient = _fixture.CreateAuthenticatedClient(AppRoles.Admin, _adminUserId);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Create_Returns201_WhenValid()
    {
        var response = await _studentClient.PostAsJsonAsync(
            BaseUrl,
            new CreateSupervisorRequestCommand(_teacherUserId, "Прошу назначить научруком"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<SupervisorRequestDto>();
        body.Should().NotBeNull();
        body!.TeacherUserId.Should().Be(_teacherUserId);
        body.Status.CodeName.Should().Be("Pending");
    }

    [Fact]
    public async Task Create_Returns400_WhenTeacherUserIdIsEmpty()
    {
        var response = await _studentClient.PostAsJsonAsync(
            BaseUrl,
            new CreateSupervisorRequestCommand(Guid.Empty, "Прошу назначить научруком"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns403_WhenCallerIsNotStudent()
    {
        var response = await _teacherClient.PostAsJsonAsync(
            BaseUrl,
            new CreateSupervisorRequestCommand(_teacherUserId, "Прошу назначить научруком"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_Returns409_WhenDuplicatePendingExists()
    {
        await CreateRequestAsync(_teacherUserId);

        var response = await _studentClient.PostAsJsonAsync(
            BaseUrl,
            new CreateSupervisorRequestCommand(_teacherUserId, "Дублирующий запрос"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Approve_CancelsOtherPendingRequests_ForSameStudent()
    {
        var request1 = await CreateRequestAsync(_teacherUserId);
        var request2 = await CreateRequestAsync(_teacher2UserId);

        var approveResponse = await _teacherClient.PutAsync($"{BaseUrl}/{request1}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail1 = await GetDetailAsync(_teacherClient, request1);
        var detail2 = await GetDetailAsync(_teacher2Client, request2);

        detail1.Status.CodeName.Should().Be("ApprovedBySupervisor");
        detail2.Status.CodeName.Should().Be("Cancelled");
    }

    [Fact]
    public async Task Approve_Returns403_WhenRequestAssignedToAnotherTeacher()
    {
        var requestId = await CreateRequestAsync(_teacherUserId);

        var response = await _teacher2Client.PutAsync($"{BaseUrl}/{requestId}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Approve_Returns400_WhenRequestIsNotPending()
    {
        var requestId = await CreateRequestAsync(_teacherUserId);
        var firstApprove = await _teacherClient.PutAsync($"{BaseUrl}/{requestId}/approve", null);
        firstApprove.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondApprove = await _teacherClient.PutAsync($"{BaseUrl}/{requestId}/approve", null);
        secondApprove.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reject_Returns400_WhenCommentEmpty()
    {
        var requestId = await CreateRequestAsync(_teacherUserId);

        var response = await _teacherClient.PutAsJsonAsync(
            $"{BaseUrl}/{requestId}/reject",
            new RejectSupervisorRequestCommand(string.Empty));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reject_Returns403_WhenRequestAssignedToAnotherTeacher()
    {
        var requestId = await CreateRequestAsync(_teacherUserId);

        var response = await _teacher2Client.PutAsJsonAsync(
            $"{BaseUrl}/{requestId}/reject",
            new RejectSupervisorRequestCommand("Не мой запрос"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cancel_Returns204_WhenStudentOwnsPendingRequest()
    {
        var requestId = await CreateRequestAsync(_teacherUserId);

        var response = await _studentClient.PutAsync($"{BaseUrl}/{requestId}/cancel", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await GetDetailAsync(_studentClient, requestId);
        detail.Status.CodeName.Should().Be("Cancelled");
    }

    [Fact]
    public async Task Cancel_Returns403_WhenStudentDoesNotOwnRequest()
    {
        var requestId = await CreateRequestAsync(_teacherUserId);

        var response = await _student2Client.PutAsync($"{BaseUrl}/{requestId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cancel_Returns409_WhenRequestIsAlreadyApproved()
    {
        var requestId = await CreateRequestAsync(_teacherUserId);
        var approveResponse = await _teacherClient.PutAsync($"{BaseUrl}/{requestId}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cancelResponse = await _studentClient.PutAsync($"{BaseUrl}/{requestId}/cancel", null);

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ListForRole_ReturnsExpectedScope()
    {
        await CreateRequestAsync(_teacherUserId);
        await CreateRequestAsync(_teacher2UserId);

        var studentResponse = await _studentClient.GetAsync(BaseUrl);
        var teacherResponse = await _teacherClient.GetAsync(BaseUrl);
        var adminResponse = await _adminClient.GetAsync(BaseUrl);

        studentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        teacherResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        adminResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var studentList = await studentResponse.Content.ReadFromJsonAsync<PagedResult<SupervisorRequestDto>>();
        var teacherList = await teacherResponse.Content.ReadFromJsonAsync<PagedResult<SupervisorRequestDto>>();
        var adminList = await adminResponse.Content.ReadFromJsonAsync<PagedResult<SupervisorRequestDto>>();

        studentList!.Total.Should().Be(2);
        teacherList!.Total.Should().Be(1);
        adminList!.Total.Should().Be(2);
    }

    [Fact]
    public async Task List_ReturnsMatching_WhenCreatedAtWithinQueryRange()
    {
        await CreateRequestAsync(_teacherUserId);
        await CreateRequestAsync(_teacher2UserId);

        var from = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var url =
            $"{BaseUrl}?createdFromUtc={Uri.EscapeDataString(from.ToString("O"))}"
            + $"&createdToUtc={Uri.EscapeDataString(to.ToString("O"))}";

        var response = await _studentClient.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<SupervisorRequestDto>>();
        body!.Total.Should().Be(2);
    }

    [Fact]
    public async Task List_ReturnsEmpty_WhenCreatedFromUtcAfterAllRequests()
    {
        await CreateRequestAsync(_teacherUserId);

        var from = new DateTimeOffset(DateTime.UtcNow.Year + 2, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var url = $"{BaseUrl}?createdFromUtc={Uri.EscapeDataString(from.ToString("O"))}";

        var response = await _studentClient.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<SupervisorRequestDto>>();
        body!.Total.Should().Be(0);
    }

    [Fact]
    public async Task List_ReturnsEmpty_WhenCreatedToUtcBeforeAllRequests()
    {
        await CreateRequestAsync(_teacherUserId);

        var to = new DateTimeOffset(1999, 12, 31, 23, 59, 59, TimeSpan.Zero);
        var url = $"{BaseUrl}?createdToUtc={Uri.EscapeDataString(to.ToString("O"))}";

        var response = await _studentClient.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<SupervisorRequestDto>>();
        body!.Total.Should().Be(0);
    }

    private async Task<Guid> CreateRequestAsync(Guid teacherUserId)
    {
        var response = await _studentClient.PostAsJsonAsync(
            BaseUrl,
            new CreateSupervisorRequestCommand(teacherUserId, "Пожалуйста, возьмите меня"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<SupervisorRequestDto>();
        return body!.Id;
    }

    private static async Task<SupervisorRequestDetailDto> GetDetailAsync(HttpClient client, Guid requestId)
    {
        var response = await client.GetAsync($"{BaseUrl}/{requestId}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SupervisorRequestDetailDto>())!;
    }

    private async Task SeedEnvironmentAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var departmentId = Guid.NewGuid();
        db.Departments.Add(new Department
        {
            Id = departmentId,
            CodeName = "test_department",
            DisplayName = "Тестовая кафедра"
        });

        var studentRoleId = Guid.NewGuid();
        var teacherRoleId = Guid.NewGuid();
        var adminRoleId = Guid.NewGuid();
        db.UserRoles.Add(new UserRole { Id = studentRoleId, CodeName = AppRoles.Student, DisplayName = "Студент" });
        db.UserRoles.Add(new UserRole { Id = teacherRoleId, CodeName = AppRoles.Teacher, DisplayName = "Преподаватель" });
        db.UserRoles.Add(new UserRole { Id = adminRoleId, CodeName = AppRoles.Admin, DisplayName = "Администратор" });

        _studentUserId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = _studentUserId,
            Email = "student-supervisor@test.com",
            PasswordHash = "x",
            FirstName = "Студент",
            LastName = "Тестовый",
            RoleId = studentRoleId,
            IsActive = true,
            DepartmentId = departmentId
        });

        _student2UserId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = _student2UserId,
            Email = "student-supervisor-2@test.com",
            PasswordHash = "x",
            FirstName = "Студент2",
            LastName = "Тестовый2",
            RoleId = studentRoleId,
            IsActive = true,
            DepartmentId = departmentId
        });

        db.Students.Add(new Student
        {
            Id = Guid.NewGuid(),
            UserId = _studentUserId,
            GroupId = await EnsureStudyGroupAsync(db, 4411)
        });

        db.Students.Add(new Student
        {
            Id = Guid.NewGuid(),
            UserId = _student2UserId,
            GroupId = await EnsureStudyGroupAsync(db, 4412)
        });

        _teacherUserId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = _teacherUserId,
            Email = "teacher-supervisor-1@test.com",
            PasswordHash = "x",
            FirstName = "Преподаватель",
            LastName = "Первый",
            RoleId = teacherRoleId,
            IsActive = true,
            DepartmentId = departmentId
        });

        _teacher2UserId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = _teacher2UserId,
            Email = "teacher-supervisor-2@test.com",
            PasswordHash = "x",
            FirstName = "Преподаватель",
            LastName = "Второй",
            RoleId = teacherRoleId,
            IsActive = true,
            DepartmentId = departmentId
        });

        _adminUserId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = _adminUserId,
            Email = "admin-supervisor@test.com",
            PasswordHash = "x",
            FirstName = "Админ",
            LastName = "Тестовый",
            RoleId = adminRoleId,
            IsActive = true
        });

        await EnsureApplicationStatusesAsync(db);
        await db.SaveChangesAsync();
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
}
