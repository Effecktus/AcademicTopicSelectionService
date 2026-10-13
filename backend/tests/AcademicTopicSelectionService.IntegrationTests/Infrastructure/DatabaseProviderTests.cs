using AcademicTopicSelectionService.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AcademicTopicSelectionService.IntegrationTests.Infrastructure;

/// <summary>
/// Гарантия для запуска набора на разных СУБД: тесты действительно идут на провайдере из <c>TEST_DB_PROVIDER</c>.
/// </summary>
[Collection(DatabaseCollection.CollectionName)]
public sealed class DatabaseProviderTests(DatabaseFixture fixture)
{
    [Fact]
    public void Application_UsesProviderFromTestSettings()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var isExpectedProvider = fixture.Provider switch
        {
            DatabaseProvider.SqlServer => db.Database.IsSqlServer(),
            _ => db.Database.IsNpgsql()
        };

        isExpectedProvider.Should().BeTrue($"TEST_DB_PROVIDER = {fixture.Provider}, фактический провайдер: {db.Database.ProviderName}");
    }

    [Fact]
    public async Task Migrations_AreAllApplied()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var pending = await db.Database.GetPendingMigrationsAsync();
        pending.Should().BeEmpty();
    }
}
