using AcademicTopicSelectionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace AcademicTopicSelectionService.IntegrationTests.Infrastructure;

/// <summary>
/// Фикстура с одним PostgreSQL-контейнером на всю коллекцию тестов.
/// Контейнер запускается один раз и удаляется после завершения всех тестов коллекции.
/// Перед каждым тестом таблицы очищаются через <see cref="ResetDatabaseAsync"/>.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("test_db")
        .Build();

    public TestWebApplicationFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Factory = new TestWebApplicationFactory(_container.GetConnectionString());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Очищает все таблицы перед каждым тестом для изоляции.
    /// Список таблиц берётся из модели EF Core — новые сущности подхватываются автоматически.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var tableNames = db.Model.GetEntityTypes()
            .Select(e => e.GetTableName())
            .Where(name => name is not null)
            .Distinct()
            .Select(name => $"\"{name}\"");

        // Имена таблиц из модели EF — SQL injection невозможна
#pragma warning disable EF1002
        await db.Database.ExecuteSqlRawAsync(
            $"TRUNCATE TABLE {string.Join(", ", tableNames)} RESTART IDENTITY CASCADE");
#pragma warning restore EF1002
    }
}

[CollectionDefinition(CollectionName)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string CollectionName = "Database";
}
