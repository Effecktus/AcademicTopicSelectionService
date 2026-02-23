using DirectoryOfGraduates.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace DirectoryOfGraduates.IntegrationTests.Infrastructure;

/// <summary>
/// Фикстура с одним PostgreSQL-контейнером на всю коллекцию тестов.
/// Контейнер запускается один раз и удаляется после завершения всех тестов коллекции.
/// Перед каждым тестом таблицы очищаются через <see cref="ResetDatabaseAsync"/>.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    // Кастомный пользователь не задаётся намеренно — используется дефолтный postgres,
    // который является superuser и может выполнять CREATE EXTENSION (citext, pgcrypto).
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
        // Миграций нет — схема создаётся прямо из модели EF Core.
        // EnsureCreatedAsync также генерирует CREATE EXTENSION для citext и pgcrypto.
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Очищает справочные таблицы перед каждым тестом для изоляции.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE \"UserRoles\", \"ApplicationStatuses\" RESTART IDENTITY CASCADE");
    }
}

[CollectionDefinition(CollectionName)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string CollectionName = "Database";
}
