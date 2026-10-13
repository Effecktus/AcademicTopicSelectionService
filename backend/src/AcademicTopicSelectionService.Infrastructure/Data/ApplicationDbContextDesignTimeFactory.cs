using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AcademicTopicSelectionService.Infrastructure.Data;

/// <summary>
/// Создание контекста для <c>dotnet ef</c> (миграции, скрипты) без запуска приложения:
/// не нужны ни секреты, ни Redis, ни S3.
/// </summary>
/// <remarks>
/// Провайдер выбирается аргументом после <c>--</c> или переменной <c>Db__Provider</c>:
/// <code>dotnet ef migrations add Name -p src/AcademicTopicSelectionService.Migrations.SqlServer -s src/AcademicTopicSelectionService.API -- --provider SqlServer</code>
/// Строка подключения нужна только командам, которые ходят в БД; её можно задать через
/// <c>ConnectionStrings__DefaultConnection</c>.
/// </remarks>
public sealed class ApplicationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var providerIndex = Array.FindIndex(args, a => a.Equals("--provider", StringComparison.OrdinalIgnoreCase));
        var providerValue = providerIndex >= 0 && providerIndex + 1 < args.Length
            ? args[providerIndex + 1]
            : Environment.GetEnvironmentVariable("Db__Provider");
        var provider = DatabaseProviderConfiguration.Parse(providerValue);

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? provider switch
            {
                DatabaseProvider.SqlServer => "Server=localhost;Database=app_database;Integrated Security=false",
                _ => "Host=localhost;Database=app_database"
            };

        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        DatabaseProviderConfiguration.Configure(options, provider, connectionString);
        return new ApplicationDbContext(options.Options);
    }
}
