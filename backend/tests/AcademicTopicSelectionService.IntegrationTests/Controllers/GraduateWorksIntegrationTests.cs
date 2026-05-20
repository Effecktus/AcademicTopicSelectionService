using System.Net;
using System.Net.Http.Json;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.GraduateWorks;
using AcademicTopicSelectionService.Application.Notifications;
using AcademicTopicSelectionService.Domain.Entities;
using AcademicTopicSelectionService.Infrastructure.Data;
using AcademicTopicSelectionService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AcademicTopicSelectionService.IntegrationTests.Controllers;

[Collection(DatabaseCollection.CollectionName)]
public sealed class GraduateWorksIntegrationTests : IAsyncLifetime
{
    private const string BaseUrl = "/api/v1/graduate-works";

    private readonly DatabaseFixture _fixture;
    private HttpClient _adminClient = null!;
    private HttpClient _studentClient = null!;

    private Guid _adminUserId;
    private Guid _studentUserId;
    private Guid _applicationId;
    private Guid _applicationId2;
    private Guid _teacherProfileId;

    public GraduateWorksIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        await SeedEnvironmentAsync();

        _adminClient = _fixture.CreateAuthenticatedClient(AppRoles.Admin, _adminUserId);
        _studentClient = _fixture.CreateAuthenticatedClient(AppRoles.Student, _studentUserId);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Create_Returns201_WhenAdminPayloadValid()
    {
        var response = await _adminClient.PostAsJsonAsync(BaseUrl, new CreateGraduateWorkCommand(
            _applicationId, "Тестовая ВКР", 2025, 90, "Иванов И.И.; Петров П.П."));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<GraduateWorkDto>();
        body.Should().NotBeNull();
        body!.ApplicationId.Should().Be(_applicationId);
        body.Title.Should().Be("Тестовая ВКР");
        body.HasFile.Should().BeFalse();
    }

    [Fact]
    public async Task Create_Returns400_WhenDuplicateApplication()
    {
        await CreateGraduateWorkAsync();

        var response = await _adminClient.PostAsJsonAsync(BaseUrl, new CreateGraduateWorkCommand(
            _applicationId, "Дублирующая ВКР", 2025, 88, "Комиссия"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns403_WhenCallerIsNotAdmin()
    {
        var response = await _studentClient.PostAsJsonAsync(BaseUrl, new CreateGraduateWorkCommand(
            _applicationId, "ВКР от студента", 2025, 80, "Комиссия"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUploadUrl_Returns403_WhenCallerIsNotAdmin()
    {
        var gwId = await CreateGraduateWorkAsync();

        var response = await _studentClient.PostAsync($"{BaseUrl}/{gwId}/upload-url/thesis", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUploadUrl_Returns200_WhenCallerIsAdmin()
    {
        var gwId = await CreateGraduateWorkAsync();

        var response = await _adminClient.PostAsync($"{BaseUrl}/{gwId}/upload-url/thesis", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FileUrlDto>();
        body.Should().NotBeNull();
        body!.Url.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ConfirmUpload_Returns204_AndPersistsFileName()
    {
        var gwId = await CreateGraduateWorkAsync();

        var response = await _adminClient.PostAsJsonAsync(
            $"{BaseUrl}/{gwId}/confirm-upload/thesis",
            new { fileName = "Инструкция UNI VPN.docx" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var entity = await db.GraduateWorks.FirstAsync(g => g.Id == gwId);

        entity.FilePath.Should().Be($"{gwId:D}/thesis");
        entity.FileName.Should().Be("Инструкция UNI VPN.docx");
    }

    [Fact]
    public async Task ConfirmUpload_CreatesGraduateWorkUploadedNotificationForStudent()
    {
        var gwId = await CreateGraduateWorkAsync();

        var confirm = await _adminClient.PostAsJsonAsync(
            $"{BaseUrl}/{gwId}/confirm-upload/thesis",
            new { fileName = "Тезисы.docx" });
        confirm.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await _studentClient.GetAsync("/api/v1/notifications?isRead=false&page=1&pageSize=50");
        list.EnsureSuccessStatusCode();
        var body = await list.Content.ReadFromJsonAsync<PagedResult<NotificationDto>>();
        body!.Items.Should().Contain(n =>
            n.TypeCodeName == "GraduateWorkUploaded" &&
            n.Content.Contains("Тезисы.docx"));
    }

    [Fact]
    public async Task ConfirmUpload_Returns400_WhenFileNameEmpty()
    {
        var gwId = await CreateGraduateWorkAsync();

        var response = await _adminClient.PostAsJsonAsync(
            $"{BaseUrl}/{gwId}/confirm-upload/thesis",
            new { fileName = "   " });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DownloadUrl_Returns400_WhenFileNotConfirmed()
    {
        var gwId = await CreateGraduateWorkAsync();

        var response = await _studentClient.GetAsync($"{BaseUrl}/{gwId}/download-url/thesis");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DownloadUrl_Returns200_WhenFileConfirmed_AndUserAuthorized()
    {
        var gwId = await CreateGraduateWorkAsync();
        var confirm = await _adminClient.PostAsJsonAsync(
            $"{BaseUrl}/{gwId}/confirm-upload/thesis",
            new { fileName = "Диплом.docx" });
        confirm.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await _studentClient.GetAsync($"{BaseUrl}/{gwId}/download-url/thesis");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FileUrlDto>();
        body.Should().NotBeNull();
        body!.Url.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task List_Returns200_AndIncludesStudentAndTeacherFullNames()
    {
        var gwId = await CreateGraduateWorkAsync();

        var response = await _adminClient.GetAsync($"{BaseUrl}?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<GraduateWorkDto>>();
        page.Should().NotBeNull();
        var item = page!.Items.Single(g => g.Id == gwId);
        item.StudentFullName.Should().Contain("Тестовый");
        item.TeacherFullName.Should().Contain("Тестовый");
    }

    [Fact]
    public async Task List_FiltersByYear()
    {
        await CreateGraduateWorkAsync(_applicationId, "ListYearAlpha", 2024, 80);
        await CreateGraduateWorkAsync(_applicationId2, "ListYearBeta", 2025, 81);

        var response = await _adminClient.GetAsync($"{BaseUrl}?page=1&pageSize=50&year=2024");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<GraduateWorkDto>>();
        page!.Items.Should().Contain(x => x.Title == "ListYearAlpha");
        page.Items.Should().NotContain(x => x.Title == "ListYearBeta");
    }

    [Fact]
    public async Task List_FiltersByTitleQuery()
    {
        await CreateGraduateWorkAsync(_applicationId, "ListTitleAlphaXyz", 2025, 80);
        await CreateGraduateWorkAsync(_applicationId2, "ListTitleBetaXyz", 2025, 81);

        var response = await _adminClient.GetAsync($"{BaseUrl}?page=1&pageSize=50&titleQuery=AlphaXyz");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<GraduateWorkDto>>();
        page!.Items.Should().Contain(x => x.Title == "ListTitleAlphaXyz");
        page.Items.Should().NotContain(x => x.Title == "ListTitleBetaXyz");
    }

    [Fact]
    public async Task List_FiltersByTeacherId()
    {
        await CreateGraduateWorkAsync(_applicationId, "ListTeacherIdA", 2025, 80);
        await CreateGraduateWorkAsync(_applicationId2, "ListTeacherIdB", 2025, 81);

        var response = await _adminClient.GetAsync($"{BaseUrl}?page=1&pageSize=50&teacherId={_teacherProfileId:D}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<GraduateWorkDto>>();
        page!.Items.Should().HaveCountGreaterThanOrEqualTo(2);
        page.Items.Should().OnlyContain(x => x.TeacherId == _teacherProfileId);
    }

    [Fact]
    public async Task List_FiltersByTeacherQuery()
    {
        await CreateGraduateWorkAsync(_applicationId, "ListTqA", 2025, 80);
        await CreateGraduateWorkAsync(_applicationId2, "ListTqB", 2025, 81);

        var response = await _adminClient.GetAsync($"{BaseUrl}?page=1&pageSize=50&teacherQuery=Преподаватель");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<GraduateWorkDto>>();
        page!.Items.Should().Contain(x => x.Title == "ListTqA" || x.Title == "ListTqB");
    }

    private Task<Guid> CreateGraduateWorkAsync() =>
        CreateGraduateWorkAsync(_applicationId, "ВКР для теста", 2025, 85);

    private async Task<Guid> CreateGraduateWorkAsync(Guid applicationId, string title, int year, int grade)
    {
        var response = await _adminClient.PostAsJsonAsync(BaseUrl, new CreateGraduateWorkCommand(
            applicationId, title, year, grade, "Иванов И.И.; Петров П.П."));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<GraduateWorkDto>();
        body.Should().NotBeNull();
        return body!.Id;
    }

    private async Task SeedEnvironmentAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var departmentId = Guid.NewGuid();
        db.Departments.Add(new Department
        {
            Id = departmentId,
            CodeName = "grad_works_test_department",
            DisplayName = "Кафедра тестов ВКР"
        });

        var adminRoleId = Guid.NewGuid();
        var studentRoleId = Guid.NewGuid();
        var teacherRoleId = Guid.NewGuid();
        db.UserRoles.Add(new UserRole { Id = adminRoleId, CodeName = AppRoles.Admin, DisplayName = "Администратор" });
        db.UserRoles.Add(new UserRole { Id = studentRoleId, CodeName = AppRoles.Student, DisplayName = "Студент" });
        db.UserRoles.Add(new UserRole { Id = teacherRoleId, CodeName = AppRoles.Teacher, DisplayName = "Преподаватель" });

        _adminUserId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = _adminUserId,
            Email = "admin.graduate-works@test.com",
            PasswordHash = "x",
            FirstName = "Админ",
            LastName = "Тестовый",
            RoleId = adminRoleId,
            IsActive = true
        });

        _studentUserId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = _studentUserId,
            Email = "student.graduate-works@test.com",
            PasswordHash = "x",
            FirstName = "Студент",
            LastName = "Тестовый",
            RoleId = studentRoleId,
            IsActive = true,
            DepartmentId = departmentId
        });

        var teacherUserId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = teacherUserId,
            Email = "teacher.graduate-works@test.com",
            PasswordHash = "x",
            FirstName = "Преподаватель",
            LastName = "Тестовый",
            RoleId = teacherRoleId,
            IsActive = true,
            DepartmentId = departmentId
        });

        var studentProfileId = Guid.NewGuid();
        db.Students.Add(new Student
        {
            Id = studentProfileId,
            UserId = _studentUserId,
            GroupId = await EnsureStudyGroupAsync(db, 5501)
        });

        var teacherProfileId = Guid.NewGuid();
        _teacherProfileId = teacherProfileId;
        db.Teachers.Add(new Teacher
        {
            Id = teacherProfileId,
            UserId = teacherUserId,
            MaxStudentsLimit = 15,
            AcademicDegreeId = await EnsureAcademicDegreeAsync(db, "None"),
            AcademicTitleId = await EnsureAcademicTitleAsync(db, "None"),
            PositionId = await EnsurePositionAsync(db, "Assistant")
        });

        var appStatusId = await EnsureApplicationStatusAsync(db, "Pending", "Ожидает");
        var topicStatusId = await EnsureTopicStatusAsync(db, "Active");
        var creatorTypeId = await EnsureTopicCreatorTypeAsync(db, "Teacher");

        var topicId = Guid.NewGuid();
        db.Topics.Add(new Topic
        {
            Id = topicId,
            Title = "Тема для интеграционных тестов ВКР",
            Description = "Описание",
            StatusId = topicStatusId,
            CreatorTypeId = creatorTypeId,
            CreatedBy = teacherUserId
        });

        var supervisorRequestId = Guid.NewGuid();
        db.SupervisorRequests.Add(new SupervisorRequest
        {
            Id = supervisorRequestId,
            StudentId = studentProfileId,
            TeacherUserId = teacherUserId,
            StatusId = appStatusId,
            Comment = "Одобрено для тестов"
        });

        _applicationId = Guid.NewGuid();
        db.StudentApplications.Add(new StudentApplication
        {
            Id = _applicationId,
            StudentId = studentProfileId,
            TopicId = topicId,
            SupervisorRequestId = supervisorRequestId,
            StatusId = appStatusId
        });

        _applicationId2 = Guid.NewGuid();
        var student2UserId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = student2UserId,
            Email = "student2.graduate-works@test.com",
            PasswordHash = "x",
            FirstName = "Второй",
            LastName = "Студент",
            RoleId = studentRoleId,
            IsActive = true,
            DepartmentId = departmentId
        });

        var studentProfileId2 = Guid.NewGuid();
        db.Students.Add(new Student
        {
            Id = studentProfileId2,
            UserId = student2UserId,
            GroupId = await EnsureStudyGroupAsync(db, 5502)
        });

        var supervisorRequestId2 = Guid.NewGuid();
        db.SupervisorRequests.Add(new SupervisorRequest
        {
            Id = supervisorRequestId2,
            StudentId = studentProfileId2,
            TeacherUserId = teacherUserId,
            StatusId = appStatusId,
            Comment = "Одобрено для тестов (второй студент)"
        });

        db.StudentApplications.Add(new StudentApplication
        {
            Id = _applicationId2,
            StudentId = studentProfileId2,
            TopicId = topicId,
            SupervisorRequestId = supervisorRequestId2,
            StatusId = appStatusId
        });

        await EnsureGraduateWorkStatusAsync(db, "Draft", "Черновик");
        await EnsureGraduateWorkStatusAsync(db, "Completed", "Заполнено");

        await NotificationTypesTestSeed.EnsureAsync(db);

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

    private static async Task<Guid> EnsureApplicationStatusAsync(ApplicationDbContext db, string codeName, string displayName)
    {
        var existing = await db.ApplicationStatuses.FirstOrDefaultAsync(s => s.CodeName == codeName);
        if (existing is not null) return existing.Id;

        var id = Guid.NewGuid();
        db.ApplicationStatuses.Add(new ApplicationStatus
        {
            Id = id,
            CodeName = codeName,
            DisplayName = displayName
        });
        await db.SaveChangesAsync();
        return id;
    }

    private static async Task<Guid> EnsureTopicStatusAsync(ApplicationDbContext db, string codeName)
    {
        var existing = await db.TopicStatuses.FirstOrDefaultAsync(s => s.CodeName == codeName);
        if (existing is not null) return existing.Id;

        var id = Guid.NewGuid();
        db.TopicStatuses.Add(new TopicStatus
        {
            Id = id,
            CodeName = codeName,
            DisplayName = codeName
        });
        await db.SaveChangesAsync();
        return id;
    }

    private static async Task<Guid> EnsureTopicCreatorTypeAsync(ApplicationDbContext db, string codeName)
    {
        var existing = await db.TopicCreatorTypes.FirstOrDefaultAsync(s => s.CodeName == codeName);
        if (existing is not null) return existing.Id;

        var id = Guid.NewGuid();
        db.TopicCreatorTypes.Add(new TopicCreatorType
        {
            Id = id,
            CodeName = codeName,
            DisplayName = codeName
        });
        await db.SaveChangesAsync();
        return id;
    }

    private static async Task<Guid> EnsureGraduateWorkStatusAsync(ApplicationDbContext db, string codeName, string displayName)
    {
        var existing = await db.GraduateWorkStatuses.FirstOrDefaultAsync(s => s.CodeName == codeName);
        if (existing is not null) return existing.Id;

        var id = Guid.NewGuid();
        db.GraduateWorkStatuses.Add(new GraduateWorkStatus
        {
            Id = id,
            CodeName = codeName,
            DisplayName = displayName
        });
        await db.SaveChangesAsync();
        return id;
    }
}
