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
    private readonly HttpClient _client;

    public TopicsControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient(AppRoles.Teacher);
    }

    public async Task InitializeAsync() => await _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task List_ReturnsEmpty_WhenNoTopics()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<TopicDto>>();
        body!.Total.Should().Be(0);
    }

    [Fact]
    public async Task List_ReturnsTopic_WhenSeeded()
    {
        var topicId = await SeedTopicAsync(title: "Моя тема ВКР");

        var response = await _client.GetAsync(BaseUrl);

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

        var response = await _client.GetAsync($"{BaseUrl}?statusCodeName=Active");

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

        var response = await _client.GetAsync($"{BaseUrl}?sort=titleAsc&pageSize=10");

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

        var response = await _client.GetAsync($"{BaseUrl}/{topicId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TopicDto>();
        body!.Id.Should().Be(topicId);
        body.CreatorType.CodeName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Get_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> SeedTopicAsync(string? title = null, string statusCodeName = "Active")
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var roleId = Guid.NewGuid();
        db.UserRoles.Add(new UserRole
        {
            Id = roleId,
            CodeName = $"R_{roleId:N}",
            DisplayName = "Роль"
        });

        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = $"u_{userId:N}@test.com",
            PasswordHash = "x",
            FirstName = "Автор",
            LastName = "Темы",
            RoleId = roleId,
            IsActive = true
        });

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
