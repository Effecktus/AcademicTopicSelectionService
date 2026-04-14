using System.Net;
using System.Net.Http.Json;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Notifications;
using AcademicTopicSelectionService.Application.SupervisorRequests;
using AcademicTopicSelectionService.Domain.Entities;
using AcademicTopicSelectionService.Infrastructure.Data;
using AcademicTopicSelectionService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AcademicTopicSelectionService.IntegrationTests.Controllers;

[Collection(DatabaseCollection.CollectionName)]
public sealed class NotificationsIntegrationTests : IAsyncLifetime
{
    private const string NotificationsBaseUrl = "/api/v1/notifications";
    private const string SupervisorRequestsBaseUrl = "/api/v1/supervisor-requests";

    private readonly DatabaseFixture _fixture;

    private HttpClient _studentClient = null!;
    private HttpClient _teacherClient = null!;

    private Guid _studentUserId;
    private Guid _teacherUserId;

    public NotificationsIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        await SeedEnvironmentAsync();

        _studentClient = _fixture.CreateAuthenticatedClient(AppRoles.Student, _studentUserId);
        _teacherClient = _fixture.CreateAuthenticatedClient(AppRoles.Teacher, _teacherUserId);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateSupervisorRequest_CreatesNotificationForTeacher()
    {
        var createResponse = await _studentClient.PostAsJsonAsync(
            SupervisorRequestsBaseUrl,
            new CreateSupervisorRequestCommand(_teacherUserId, "Прошу взять на научное руководство"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await _teacherClient.GetAsync($"{NotificationsBaseUrl}?isRead=false&page=1&pageSize=50");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await listResponse.Content.ReadFromJsonAsync<PagedResult<NotificationDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().Contain(n =>
            n.TypeCodeName == "SupervisorRequestCreated" &&
            n.Title == "Новый запрос на научное руководство");
    }

    [Fact]
    public async Task MarkAsRead_ReturnsForbidden_ForForeignNotification()
    {
        var teacherNotificationId = await CreateNotificationAsync(
            _teacherUserId,
            "SupervisorRequestCreated",
            "Новый запрос на научное руководство",
            "Контент");

        var response = await _studentClient.PutAsync($"{NotificationsBaseUrl}/{teacherNotificationId}/read", null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MarkAllAsRead_UpdatesOnlyCurrentUserNotifications()
    {
        await CreateNotificationAsync(_teacherUserId, "SupervisorRequestCreated", "T1", "C1");
        await CreateNotificationAsync(_teacherUserId, "SupervisorRequestStatusChanged", "T2", "C2");
        await CreateNotificationAsync(_studentUserId, "SupervisorRequestStatusChanged", "S1", "C3");

        var response = await _teacherClient.PutAsync($"{NotificationsBaseUrl}/read-all", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var teacherUnread = await db.Notifications.CountAsync(n => n.UserId == _teacherUserId && !n.IsRead);
        var studentUnread = await db.Notifications.CountAsync(n => n.UserId == _studentUserId && !n.IsRead);

        teacherUnread.Should().Be(0);
        studentUnread.Should().Be(1);
    }

    private async Task SeedEnvironmentAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var studentRoleId = Guid.NewGuid();
        var teacherRoleId = Guid.NewGuid();
        db.UserRoles.Add(new UserRole { Id = studentRoleId, CodeName = AppRoles.Student, DisplayName = "Студент" });
        db.UserRoles.Add(new UserRole { Id = teacherRoleId, CodeName = AppRoles.Teacher, DisplayName = "Преподаватель" });

        _studentUserId = Guid.NewGuid();
        _teacherUserId = Guid.NewGuid();

        db.Users.Add(new User
        {
            Id = _studentUserId,
            Email = "student.notifications@test.com",
            PasswordHash = "x",
            FirstName = "Студент",
            LastName = "Тестов",
            RoleId = studentRoleId,
            IsActive = true
        });

        db.Users.Add(new User
        {
            Id = _teacherUserId,
            Email = "teacher.notifications@test.com",
            PasswordHash = "x",
            FirstName = "Преподаватель",
            LastName = "Тестов",
            RoleId = teacherRoleId,
            IsActive = true
        });

        db.Students.Add(new Student
        {
            Id = Guid.NewGuid(),
            UserId = _studentUserId,
            GroupId = await EnsureStudyGroupAsync(db, 4411)
        });

        await EnsureApplicationStatusAsync(db, "Pending", "Ожидает");
        await EnsureNotificationTypeAsync(db, "SupervisorRequestCreated", "Новый запрос на научное руководство");
        await EnsureNotificationTypeAsync(db, "SupervisorRequestStatusChanged", "Статус запроса на научрука изменен");

        await db.SaveChangesAsync();
    }

    private async Task<Guid> CreateNotificationAsync(Guid userId, string typeCodeName, string title, string content)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var typeId = await db.NotificationTypes
            .Where(t => t.CodeName == typeCodeName)
            .Select(t => t.Id)
            .FirstAsync();

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TypeId = typeId,
            Title = title,
            Content = content,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        return notification.Id;
    }

    private static async Task EnsureApplicationStatusAsync(ApplicationDbContext db, string codeName, string displayName)
    {
        if (!await db.ApplicationStatuses.AnyAsync(s => s.CodeName == codeName))
        {
            db.ApplicationStatuses.Add(new ApplicationStatus
            {
                Id = Guid.NewGuid(),
                CodeName = codeName,
                DisplayName = displayName
            });
        }
    }

    private static async Task EnsureNotificationTypeAsync(ApplicationDbContext db, string codeName, string displayName)
    {
        if (!await db.NotificationTypes.AnyAsync(t => t.CodeName == codeName))
        {
            db.NotificationTypes.Add(new NotificationType
            {
                Id = Guid.NewGuid(),
                CodeName = codeName,
                DisplayName = displayName
            });
        }
    }

    private static async Task<Guid> EnsureStudyGroupAsync(ApplicationDbContext db, int codeName)
    {
        var existing = await db.StudyGroups.FirstOrDefaultAsync(g => g.CodeName == codeName);
        if (existing is not null)
            return existing.Id;

        var id = Guid.NewGuid();
        db.StudyGroups.Add(new StudyGroup { Id = id, CodeName = codeName });
        return id;
    }
}
