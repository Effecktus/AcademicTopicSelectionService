using System.Net;
using System.Net.Http.Json;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Teachers;
using AcademicTopicSelectionService.Domain.Entities;
using AcademicTopicSelectionService.Infrastructure.Data;
using AcademicTopicSelectionService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AcademicTopicSelectionService.IntegrationTests.Controllers;

[Collection(DatabaseCollection.CollectionName)]
public sealed class TeachersControllerTests : IAsyncLifetime
{
    private const string BaseUrl = "/api/v1/teachers";

    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public TeachersControllerTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient(AppRoles.Student);
    }

    public async Task InitializeAsync() => await _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task List_ReturnsEmpty_WhenNoTeachers()
    {
        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<TeacherDto>>();
        body!.Total.Should().Be(0);
        body.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task List_ReturnsTeacher_WhenSeeded()
    {
        var teacherId = await SeedTeacherAsync();

        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<TeacherDto>>();
        body!.Total.Should().Be(1);
        body.Items.Should().ContainSingle(t => t.Id == teacherId);
    }

    [Fact]
    public async Task List_ExcludesInactiveUser()
    {
        await SeedTeacherAsync(isActive: false);

        var response = await _client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<TeacherDto>>();
        body!.Total.Should().Be(0);
    }

    [Fact]
    public async Task List_FiltersByQuery()
    {
        await SeedTeacherAsync(email: "unique_teacher_query@test.com", lastName: "УникальнаяФамилия");

        var response = await _client.GetAsync($"{BaseUrl}?query=unique_teacher_query");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<TeacherDto>>();
        body!.Total.Should().Be(1);
        body.Items[0].Email.Should().Be("unique_teacher_query@test.com");
    }

    [Fact]
    public async Task Get_ReturnsTeacher_WhenExists()
    {
        var teacherId = await SeedTeacherAsync();

        var response = await _client.GetAsync($"{BaseUrl}/{teacherId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TeacherDto>();
        body!.Id.Should().Be(teacherId);
        body.AcademicDegree.CodeName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Get_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> SeedTeacherAsync(string? email = null, bool isActive = true, string? lastName = null)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var degreeId = Guid.NewGuid();
        db.AcademicDegrees.Add(new AcademicDegree
        {
            Id = degreeId,
            CodeName = $"Deg_{degreeId:N}",
            DisplayName = "Степень"
        });

        var titleId = Guid.NewGuid();
        db.AcademicTitles.Add(new AcademicTitle
        {
            Id = titleId,
            CodeName = $"Tit_{titleId:N}",
            DisplayName = "Звание"
        });

        var posId = Guid.NewGuid();
        db.Positions.Add(new Position
        {
            Id = posId,
            CodeName = $"Pos_{posId:N}",
            DisplayName = "Должность"
        });

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
            Email = email ?? $"teacher_{userId:N}@test.com",
            PasswordHash = "x",
            FirstName = "Иван",
            LastName = lastName ?? "Петров",
            MiddleName = null,
            RoleId = roleId,
            IsActive = isActive
        });

        var teacherId = Guid.NewGuid();
        db.Teachers.Add(new Teacher
        {
            Id = teacherId,
            UserId = userId,
            MaxStudentsLimit = 3,
            AcademicDegreeId = degreeId,
            AcademicTitleId = titleId,
            PositionId = posId
        });

        await db.SaveChangesAsync();
        return teacherId;
    }
}
