using System.Net.Http.Headers;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using StackExchange.Redis;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace AcademicTopicSelectionService.IntegrationTests.Infrastructure;

/// <summary>
/// Фикстура с контейнерами БД и Redis на всю коллекцию тестов.
/// Контейнеры запускаются один раз и удаляются после завершения всех тестов коллекции.
/// Перед каждым тестом таблицы и Redis очищаются через методы Reset*.
/// </summary>
/// <remarks>
/// СУБД выбирается переменной окружения <c>TEST_DB_PROVIDER</c>: <c>Postgres</c> (по умолчанию) или <c>SqlServer</c>.
/// </remarks>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private const string TestDatabaseName = "test_db";

    public DatabaseProvider Provider { get; } =
        DatabaseProviderConfiguration.Parse(Environment.GetEnvironmentVariable("TEST_DB_PROVIDER"));

    private readonly IDatabaseContainer _dbContainer;

    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7-alpine")
        .Build();

    public TestWebApplicationFactory Factory { get; private set; } = null!;

    public DatabaseFixture()
    {
        _dbContainer = Provider switch
        {
            DatabaseProvider.SqlServer => new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU26-ubuntu-22.04").Build(),
            _ => new PostgreSqlBuilder("postgres:16.8-alpine").WithDatabase(TestDatabaseName).Build()
        };
    }

    public async Task InitializeAsync()
    {
        // Устанавливаем среду ДО создания фабрики: WebApplication.CreateBuilder читает
        // ASPNETCORE_ENVIRONMENT при инициализации и загружает appsettings.Testing.json,
        // содержащий Jwt:SecretKey. Без этого Program.cs бросает исключение до Build().
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        await Task.WhenAll(_dbContainer.StartAsync(), _redisContainer.StartAsync());

        Factory = new TestWebApplicationFactory(
            Provider,
            GetDatabaseConnectionString(),
            _redisContainer.GetConnectionString());

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // Схема создаётся теми же миграциями, что и в dev/prod, — тесты заодно проверяют сами миграции.
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await Task.WhenAll(_dbContainer.DisposeAsync().AsTask(), _redisContainer.DisposeAsync().AsTask());
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
            .ToList();

        // SQL Server не позволяет TRUNCATE таблиц, на которые ссылаются FK: отключаем ограничения и удаляем строки.
        var sql = Provider == DatabaseProvider.SqlServer
            ? string.Concat(tableNames.Select(t => $"ALTER TABLE [{t}] NOCHECK CONSTRAINT ALL;"))
              + string.Concat(tableNames.Select(t => $"DELETE FROM [{t}];"))
              + string.Concat(tableNames.Select(t => $"ALTER TABLE [{t}] WITH CHECK CHECK CONSTRAINT ALL;"))
            : $"TRUNCATE TABLE {string.Join(", ", tableNames.Select(t => $"\"{t}\""))} RESTART IDENTITY CASCADE";

        // Имена таблиц из модели EF — SQL injection невозможна
#pragma warning disable EF1002
        await db.Database.ExecuteSqlRawAsync(sql);
#pragma warning restore EF1002

        // Справочники пересоздаются с новыми Guid — сбрасываем IMemoryCache (кэш id статусов и т.п.).
        var memoryCache = scope.ServiceProvider.GetService<IMemoryCache>();
        if (memoryCache is MemoryCache mc)
            mc.Compact(1.0);
    }

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

    /// <summary>
    /// HTTP-клиент с заголовком <c>Authorization: Bearer</c>, указанной ролью и userId в JWT.
    /// <c>userId</c> должен соответствовать <c>Users.Id</c> в базе данных.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string role, Guid userId)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", IntegrationTestJwt.CreateAccessToken(role, userId));
        return client;
    }

    private string GetDatabaseConnectionString()
    {
        var connectionString = _dbContainer.GetConnectionString();
        if (Provider != DatabaseProvider.SqlServer)
            return connectionString;

        // Контейнер отдаёт строку на master; отдельную базу с нужной collation создаёт MigrateAsync.
        return new SqlConnectionStringBuilder(connectionString) { InitialCatalog = TestDatabaseName }.ConnectionString;
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
