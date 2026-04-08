using System.Net.Http.Headers;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace AcademicTopicSelectionService.IntegrationTests.Infrastructure;

/// <summary>
/// Фикстура с PostgreSQL и Redis контейнерами на всю коллекцию тестов.
/// Контейнеры запускаются один раз и удаляются после завершения всех тестов коллекции.
/// Перед каждым тестом таблицы и Redis очищаются через методы Reset*.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pgContainer = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("test_db")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7-alpine")
        .Build();

    public TestWebApplicationFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Устанавливаем среду ДО создания фабрики: WebApplication.CreateBuilder читает
        // ASPNETCORE_ENVIRONMENT при инициализации и загружает appsettings.Testing.json,
        // содержащий Jwt:SecretKey. Без этого Program.cs бросает исключение до Build().
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        await Task.WhenAll(_pgContainer.StartAsync(), _redisContainer.StartAsync());

        Factory = new TestWebApplicationFactory(
            _pgContainer.GetConnectionString(),
            _redisContainer.GetConnectionString());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await Task.WhenAll(_pgContainer.DisposeAsync().AsTask(), _redisContainer.DisposeAsync().AsTask());
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
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

    /// <summary>
    /// Очищает все ключи Redis перед тестами авторизации для изоляции refresh-токенов.
    /// Использует отдельное admin-соединение, так как FLUSHDB требует allowAdmin=true.
    /// </summary>
    /// <summary>
    /// HTTP-клиент с заголовком <c>Authorization: Bearer</c> и указанной ролью в JWT (для проверок авторизации).
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string role)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", IntegrationTestJwt.CreateAccessToken(role));
        return client;
    }

    public async Task ResetRedisAsync()
    {
        var adminCs = _redisContainer.GetConnectionString() + ",allowAdmin=true";
        using var adminRedis = await ConnectionMultiplexer.ConnectAsync(adminCs);
        var server = adminRedis.GetServer(adminRedis.GetEndPoints()[0]);
        await server.FlushDatabaseAsync();
    }
}

[CollectionDefinition(CollectionName)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string CollectionName = "Database";
}
