using System.Net;
using System.Net.Http.Json;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.GraduateWorks;
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

        entity.FilePath.Should().Be($"graduate-works/{gwId:D}/thesis");
        entity.FileName.Should().Be("Инструкция UNI VPN.docx");
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

    private async Task<Guid> CreateGraduateWorkAsync()
    {
        var response = await _adminClient.PostAsJsonAsync(BaseUrl, new CreateGraduateWorkCommand(
            _applicationId, "ВКР для теста", 2025, 85, "Иванов И.И.; Петров П.П."));
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
}
