using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using AcademicTopicSelectionService.Infrastructure.Data;
using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Infrastructure.Repositories;

namespace AcademicTopicSelectionService.Infrastructure;

/// <summary>
/// Методы расширения для регистрации сервисов слоя Infrastructure в DI-контейнере.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует все инфраструктурные сервисы (репозитории, DbContext) в контейнере зависимостей.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configuration">Конфигурация приложения для получения строки подключения.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Репозитории справочников
        services.AddScoped<IUserRolesRepository, UserRolesRepository>();
        services.AddScoped<IApplicationStatusesRepository, ApplicationStatusesRepository>();
        services.AddScoped<ITopicStatusesRepository, TopicStatusesRepository>();
        services.AddScoped<INotificationTypesRepository, NotificationTypesRepository>();
        services.AddScoped<IAcademicDegreesRepository, AcademicDegreesRepository>();
        services.AddScoped<IAcademicTitlesRepository, AcademicTitlesRepository>();
        services.AddScoped<IPositionsRepository, PositionsRepository>();

        // Контекст базы данных PostgreSQL
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = BuildPostgresConnectionString(configuration);
            options.UseNpgsql(connectionString);
        });

        return services;
    }

    /// <summary>
    /// Формирует строку подключения к PostgreSQL с поддержкой чтения пароля из файла.
    /// </summary>
    /// <param name="config">Конфигурация приложения.</param>
    /// <returns>Готовая строка подключения.</returns>
    /// <exception cref="InvalidOperationException">Если строка подключения не указана или файл пароля пуст.</exception>
    /// <exception cref="FileNotFoundException">Если файл пароля не найден.</exception>
    /// <remarks>
    /// Поддерживает два способа указания пароля:
    /// <list type="bullet">
    ///   <item>Непосредственно в строке подключения (ConnectionStrings:DefaultConnection)</item>
    ///   <item>В отдельном файле (Db:PasswordFile) — для безопасного хранения секретов</item>
    /// </list>
    /// </remarks>
    private static string BuildPostgresConnectionString(IConfiguration config)
    {
        var cs = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(cs))
        {
            throw new InvalidOperationException("Missing connection string: ConnectionStrings:DefaultConnection");
        }

        var csb = new NpgsqlConnectionStringBuilder(cs);

        // Если пароль уже указан в строке подключения — используем его
        if (!string.IsNullOrWhiteSpace(csb.Password))
        {
            return csb.ConnectionString;
        }

        // Пытаемся прочитать пароль из файла
        var passwordFile = config["Db:PasswordFile"];
        if (string.IsNullOrWhiteSpace(passwordFile))
        {
            return csb.ConnectionString;
        }

        if (!File.Exists(passwordFile))
        {
            throw new FileNotFoundException($"Password file not found: '{passwordFile}'", passwordFile);
        }

        var password = File.ReadAllText(passwordFile).Trim();
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException($"Password file is empty: '{passwordFile}'");
        }

        csb.Password = password;
        return csb.ConnectionString;
    }
}

