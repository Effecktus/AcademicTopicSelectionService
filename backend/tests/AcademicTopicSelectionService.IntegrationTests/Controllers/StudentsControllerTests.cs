using System.Net;
using System.Net.Http.Json;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Students;
using AcademicTopicSelectionService.Domain.Entities;
using AcademicTopicSelectionService.Infrastructure.Data;
using AcademicTopicSelectionService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AcademicTopicSelectionService.IntegrationTests.Controllers;

[Collection(DatabaseCollection.CollectionName)]
public sealed class StudentsControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "/api/v1/students";

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public StudentsControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient(AppRoles.Admin);
    }

    public async Task InitializeAsync() => await _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task List_ReturnsEmpty_WhenNoStudents()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<StudentDto>>();
        body!.Total.Should().Be(0);
    }

    [Fact]
    public async Task List_ReturnsStudent_WhenSeeded()
    {
        var studentId = await SeedStudentAsync();

        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<StudentDto>>();
        body!.Total.Should().Be(1);
        body.Items[0].Id.Should().Be(studentId);
    }

    [Fact]
    public async Task List_FiltersByGroupId()
    {
        var groupA = await SeedStudyGroupAsync(5101);
        var groupB = await SeedStudyGroupAsync(5102);
        await SeedStudentAsync(groupId: groupA);
        await SeedStudentAsync(groupId: groupB);

        var response = await _client.GetAsync($"{BaseUrl}?groupId={groupA}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<StudentDto>>();
        body!.Total.Should().Be(1);
        body.Items[0].StudyGroup.Id.Should().Be(groupA);
    }

    [Fact]
    public async Task Get_ReturnsStudent_WhenExists()
    {
        var studentId = await SeedStudentAsync();

        var response = await _client.GetAsync($"{BaseUrl}/{studentId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<StudentDto>();
        body!.Id.Should().Be(studentId);
        body.StudyGroup.CodeName.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Get_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> SeedStudyGroupAsync(int codeName)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var id = Guid.NewGuid();
        db.StudyGroups.Add(new StudyGroup { Id = id, CodeName = codeName });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedStudentAsync(Guid? groupId = null)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var gId = groupId ?? await SeedStudyGroupAsync(5200);

        var roleId = Guid.NewGuid();
        db.UserRoles.Add(new UserRole
        {
            Id = roleId,
            CodeName = $"Sr_{roleId:N}",
            DisplayName = "Студент"
        });

        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = $"st_{userId:N}@test.com",
            PasswordHash = "x",
            FirstName = "Студент",
            LastName = "Тестовый",
            RoleId = roleId,
            IsActive = true
        });

        var studentId = Guid.NewGuid();
        db.Students.Add(new Student
        {
            Id = studentId,
            UserId = userId,
            GroupId = gId
        });

        await db.SaveChangesAsync();
        return studentId;
    }
}
