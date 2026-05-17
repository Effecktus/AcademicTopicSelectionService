using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.Application.Admin;
using AcademicTopicSelectionService.Domain.Entities;
using AcademicTopicSelectionService.Infrastructure.Data;
using AcademicTopicSelectionService.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AcademicTopicSelectionService.IntegrationTests.Controllers;

[Collection(DatabaseCollection.CollectionName)]
public sealed class AdminAnalyticsIntegrationTests : IAsyncLifetime
{
    private const string AnalyticsUrl = "/api/v1/admin/analytics";

    private readonly DatabaseFixture _fixture;
    private HttpClient _adminClient = null!;

    private Guid _adminUserId;

    public AdminAnalyticsIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        await SeedEnvironmentAsync();

        _adminClient = _fixture.CreateAuthenticatedClient(AppRoles.Admin, _adminUserId);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAnalytics_Returns200_WithSummaryFields()
    {
        var response = await _adminClient.GetAsync(AnalyticsUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AdminAnalyticsDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.Should().NotBeNull();
        body!.Summary.Should().NotBeNull();
        body.Summary.TotalApplications.Should().BeGreaterThanOrEqualTo(0);
        body.Summary.TotalGraduateWorks.Should().BeGreaterThanOrEqualTo(0);
        body.Summary.TotalUsers.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetAnalytics_Returns401_WhenUnauthenticated()
    {
        var anonymousClient = _fixture.Factory.CreateClient();

        var response = await anonymousClient.GetAsync(AnalyticsUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAnalytics_Summary_ReflectsSeededData()
    {
        // SeedEnvironmentAsync already creates one admin user and one extra student user.
        var response = await _adminClient.GetAsync(AnalyticsUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AdminAnalyticsDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        body.Should().NotBeNull();
        body!.Summary.TotalUsers.Should().BeGreaterThanOrEqualTo(1,
            "at least the seeded admin and student users must be counted");
    }

    // -------------------------------------------------------------------------
    // Seed
    // -------------------------------------------------------------------------

    private async Task SeedEnvironmentAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var adminRoleId = Guid.NewGuid();
        var studentRoleId = Guid.NewGuid();
        db.UserRoles.Add(new UserRole { Id = adminRoleId, CodeName = AppRoles.Admin, DisplayName = "Администратор" });
        db.UserRoles.Add(new UserRole { Id = studentRoleId, CodeName = AppRoles.Student, DisplayName = "Студент" });

        _adminUserId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = _adminUserId,
            Email = "admin.analytics@test.com",
            PasswordHash = "x",
            FirstName = "Админ",
            LastName = "Аналитик",
            RoleId = adminRoleId,
            IsActive = true
        });

        // Seed one extra student user so TotalUsers >= 1 is reliable
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "student.analytics@test.com",
            PasswordHash = "x",
            FirstName = "Студент",
            LastName = "Аналитик",
            RoleId = studentRoleId,
            IsActive = true
        });

        await db.SaveChangesAsync();
    }
}
