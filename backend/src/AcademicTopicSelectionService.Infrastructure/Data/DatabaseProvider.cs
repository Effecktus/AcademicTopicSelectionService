using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Data;

/// <summary>
/// Поддерживаемые СУБД. Выбирается настройкой <c>Db:Provider</c> (по умолчанию PostgreSQL).
/// </summary>
public enum DatabaseProvider
{
    Postgres,
    SqlServer
}

/// <summary>
/// Подключение <see cref="ApplicationDbContext"/> к выбранной СУБД.
/// Общее для приложения и design-time фабрики <c>dotnet ef</c>.
/// </summary>
public static class DatabaseProviderConfiguration
{
    /// <summary>Сборка с миграциями для PostgreSQL.</summary>
    public const string PostgresMigrationsAssembly = "AcademicTopicSelectionService.Migrations.Postgres";

    /// <summary>Сборка с миграциями для SQL Server.</summary>
    public const string SqlServerMigrationsAssembly = "AcademicTopicSelectionService.Migrations.SqlServer";

    /// <summary>
    /// Разбирает значение <c>Db:Provider</c>; пустое значение — PostgreSQL.
    /// </summary>
    /// <exception cref="InvalidOperationException">Если провайдер неизвестен.</exception>
    public static DatabaseProvider Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DatabaseProvider.Postgres;

        return Enum.TryParse<DatabaseProvider>(value, ignoreCase: true, out var provider)
            ? provider
            : throw new InvalidOperationException(
                $"Unknown Db:Provider '{value}'. Supported: {string.Join(", ", Enum.GetNames<DatabaseProvider>())}");
    }

    /// <summary>
    /// Настраивает провайдер EF Core, повтор при временных сбоях и сборку миграций.
    /// </summary>
    /// <remarks>
    /// Повтор при сбоях (обрыв соединения, 57P01 после pg_terminate_backend, KILL в SQL Server и т.п.):
    /// явные транзакции (BeginTransaction) нужно оборачивать в
    /// <c>Database.CreateExecutionStrategy().ExecuteAsync(...)</c>, иначе EF бросит исключение.
    /// </remarks>
    public static void Configure(DbContextOptionsBuilder options, DatabaseProvider provider, string connectionString)
    {
        switch (provider)
        {
            case DatabaseProvider.Postgres:
                options.UseNpgsql(connectionString, npgsql => npgsql
                    .EnableRetryOnFailure()
                    .MigrationsAssembly(PostgresMigrationsAssembly));
                break;
            case DatabaseProvider.SqlServer:
                options.UseSqlServer(connectionString, sqlServer => sqlServer
                    .EnableRetryOnFailure()
                    .MigrationsAssembly(SqlServerMigrationsAssembly));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
        }
    }
}
