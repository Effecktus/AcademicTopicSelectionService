using System.Net;
using System.Net.Http.Json;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Topics;
using AcademicTopicSelectionService.Domain.Entities;
using AcademicTopicSelectionService.Infrastructure.Data;
using AcademicTopicSelectionService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AcademicTopicSelectionService.IntegrationTests.Controllers;

[Collection(DatabaseCollection.CollectionName)]
public sealed class TopicsControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "/api/v1/topics";

    private readonly DatabaseFixture _fixture;
    private HttpClient _teacherClient = null!;
    private HttpClient _otherTeacherClient = null!;
    private Guid _teacherUserId;
    private Guid _otherTeacherUserId;

    public TopicsControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _teacherUserId = Guid.NewGuid();
        _otherTeacherUserId = Guid.NewGuid();
        _teacherClient = _fixture.CreateAuthenticatedClient(AppRoles.Teacher, _teacherUserId);
        _otherTeacherClient = _fixture.CreateAuthenticatedClient(AppRoles.Teacher, _otherTeacherUserId);
    }
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task List_ReturnsEmpty_WhenNoTopics()
    {
        var response = await _teacherClient.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<TopicDto>>();
        body!.Total.Should().Be(0);
    }

    [Fact]
    public async Task List_ReturnsTopic_WhenSeeded()
    {
        var topicId = await SeedTopicAsync(title: "Моя тема ВКР");

        var response = await _teacherClient.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<TopicDto>>();
        body!.Total.Should().Be(1);
        body.Items[0].Id.Should().Be(topicId);
        body.Items[0].Title.Should().Be("Моя тема ВКР");
    }

    [Fact]
    public async Task List_FiltersByStatusCodeName()
    {
        await SeedTopicAsync(statusCodeName: "Active");
        await SeedTopicAsync(statusCodeName: "Inactive");

        var response = await _teacherClient.GetAsync($"{BaseUrl}?statusCodeName=Active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<TopicDto>>();
        body!.Total.Should().Be(1);
        body.Items[0].Status.CodeName.Should().Be("Active");
    }

    [Fact]
    public async Task List_SortsByTitleAsc()
    {
        await SeedTopicAsync(title: "Бета-тема");
        await SeedTopicAsync(title: "Альфа-тема");

        var response = await _teacherClient.GetAsync($"{BaseUrl}?sort=titleAsc&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<TopicDto>>();
        body!.Items.Should().HaveCount(2);
        body.Items[0].Title.Should().Be("Альфа-тема");
        body.Items[1].Title.Should().Be("Бета-тема");
    }

    [Fact]
    public async Task Get_ReturnsTopic_WhenExists()
    {
        var topicId = await SeedTopicAsync();

        var response = await _teacherClient.GetAsync($"{BaseUrl}/{topicId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TopicDto>();
        body!.Id.Should().Be(topicId);
        body.CreatorType.CodeName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Get_Returns404_WhenNotFound()
    {
        var response = await _teacherClient.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Returns201_WhenValid()
    {
        await EnsureUserExistsAsync(_teacherUserId);
        await EnsureTopicsDictionariesAsync();

        var response = await _teacherClient.PostAsJsonAsync(BaseUrl, new
        {
            Title = "Новая тема от teacher",
            Description = "Описание",
            CreatorTypeCodeName = "Teacher",
            StatusCodeName = "Active"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_Returns400_WhenTitleIsEmpty()
    {
        await EnsureUserExistsAsync(_teacherUserId);
        await EnsureTopicsDictionariesAsync();

        var response = await _teacherClient.PostAsJsonAsync(BaseUrl, new
        {
            Title = "",
            Description = "Описание",
            CreatorTypeCodeName = "Teacher",
            StatusCodeName = "Active"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Replace_Returns403_WhenCallerIsNotAuthor()
    {
        var topicId = await SeedTopicAsync(createdByUserId: _teacherUserId);

        var response = await _otherTeacherClient.PutAsJsonAsync($"{BaseUrl}/{topicId}", new
        {
            Title = "Переименование",
            Description = "Описание",
            StatusCodeName = "Active"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Patch_Returns403_WhenCallerIsNotAuthor()
    {
        var topicId = await SeedTopicAsync(createdByUserId: _teacherUserId);

        var response = await _otherTeacherClient.PatchAsJsonAsync($"{BaseUrl}/{topicId}", new
        {
            Title = "Новый title"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_Returns204_WhenCallerIsAuthorAndNoApplications()
    {
        var topicId = await SeedTopicAsync(createdByUserId: _teacherUserId);

        var response = await _teacherClient.DeleteAsync($"{BaseUrl}/{topicId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_Returns403_WhenCallerIsNotAuthor()
    {
        var topicId = await SeedTopicAsync(createdByUserId: _teacherUserId);

        var response = await _otherTeacherClient.DeleteAsync($"{BaseUrl}/{topicId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<Guid> SeedTopicAsync(
        string? title = null,
        string statusCodeName = "Active",
        Guid? createdByUserId = null)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var userId = createdByUserId ?? Guid.NewGuid();
        await EnsureUserExistsInternalAsync(db, userId);

        var topicStatusId = await EnsureTopicStatusAsync(db, statusCodeName);
        var creatorTypeId = await EnsureTopicCreatorTypeAsync(db, "Teacher");

        var topicId = Guid.NewGuid();
        db.Topics.Add(new Topic
        {
            Id = topicId,
            Title = title ?? $"Тема {topicId:N}",
            Description = null,
            StatusId = topicStatusId,
            CreatorTypeId = creatorTypeId,
            CreatedBy = userId
        });

        await db.SaveChangesAsync();
        return topicId;
    }

    private async Task EnsureUserExistsAsync(Guid userId)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await EnsureUserExistsInternalAsync(db, userId);
        await db.SaveChangesAsync();
    }

    private static async Task EnsureUserExistsInternalAsync(ApplicationDbContext db, Guid userId)
    {
        if (await db.Users.FindAsync(userId) is not null)
            return;

        var roleCode = "Teacher";
        var role = await db.UserRoles.FirstOrDefaultAsync(r => r.CodeName == roleCode);
        if (role is null)
        {
            role = new UserRole { Id = Guid.NewGuid(), CodeName = roleCode, DisplayName = "Преподаватель" };
            db.UserRoles.Add(role);
            await db.SaveChangesAsync();
        }

        db.Users.Add(new User
        {
            Id = userId,
            Email = $"u_{userId:N}@test.com",
            PasswordHash = "x",
            FirstName = "Автор",
            LastName = "Темы",
            RoleId = role.Id,
            IsActive = true
        });
    }

    private async Task EnsureTopicsDictionariesAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await EnsureTopicCreatorTypeAsync(db, "Teacher");
        await EnsureTopicCreatorTypeAsync(db, "Student");
        await EnsureTopicStatusAsync(db, "Active");
        await EnsureTopicStatusAsync(db, "Inactive");
    }

    private static async Task<Guid> EnsureTopicStatusAsync(ApplicationDbContext db, string codeName)
    {
        var existing = await db.TopicStatuses.FirstOrDefaultAsync(s => s.CodeName == codeName);
        if (existing is not null)
        {
            return existing.Id;
        }

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
        if (existing is not null)
        {
            return existing.Id;
        }

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
