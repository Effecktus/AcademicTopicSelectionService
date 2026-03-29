using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StackExchange.Redis;
using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Infrastructure.Auth;
using AcademicTopicSelectionService.Infrastructure.Data;
using AcademicTopicSelectionService.Infrastructure.Repositories;

namespace AcademicTopicSelectionService.Infrastructure;

/// <summary>
/// Методы расширения для регистрации сервисов слоя Infrastructure в DI-контейнере.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует все инфраструктурные сервисы (репозитории, DbContext, Redis) в контейнере зависимостей.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configuration">Конфигурация приложения для получения строки подключения.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // JWT-настройки
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var redisConnectionString = BuildRedisConnectionString(configuration);
            var configOptions = ConfigurationOptions.Parse(redisConnectionString);
            // abortConnect=true: если Redis недоступен при старте — сразу падаем,
            // иначе ошибки будут молчаливыми (команды возвращают null/false).
            configOptions.AbortOnConnectFail = true;
            return ConnectionMultiplexer.Connect(configOptions);
        });

        // Инфраструктурные сервисы аутентификации
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IRefreshTokenCache, RedisRefreshTokenCache>();

        // Инфраструктурные сервисы
        services.AddScoped<IDatabaseHealthChecker, Data.DatabaseHealthChecker>();

        // Репозитории
        services.AddScoped<IUsersRepository, UsersRepository>();

        // Репозитории справочников
        services.AddScoped<IUserRolesRepository, UserRolesRepository>();
        services.AddScoped<IApplicationStatusesRepository, ApplicationStatusesRepository>();
        services.AddScoped<ITopicStatusesRepository, TopicStatusesRepository>();
        services.AddScoped<ITopicCreatorTypesRepository, TopicCreatorTypesRepository>();
        services.AddScoped<INotificationTypesRepository, NotificationTypesRepository>();
        services.AddScoped<IAcademicDegreesRepository, AcademicDegreesRepository>();
        services.AddScoped<IAcademicTitlesRepository, AcademicTitlesRepository>();
        services.AddScoped<IPositionsRepository, PositionsRepository>();
        services.AddScoped<IStudyGroupsRepository, StudyGroupsRepository>();

        // Контекст базы данных PostgreSQL
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = BuildPostgresConnectionString(configuration);
            options.UseNpgsql(connectionString);
        });

        return services;
    }

    /// <summary>
    /// Формирует строку подключения к Redis с поддержкой чтения пароля из файла.
    /// </summary>
    /// <exception cref="InvalidOperationException">Если <c>Redis:ConnectionString</c> не задана.</exception>
    private static string BuildRedisConnectionString(IConfiguration config)
    {
        var cs = config["Redis:ConnectionString"];
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("Missing Redis connection string: Redis:ConnectionString");

        var passwordFile = config["Redis:PasswordFile"];
        if (string.IsNullOrWhiteSpace(passwordFile))
            return cs;

        if (!File.Exists(passwordFile))
            throw new FileNotFoundException($"Redis password file not found: '{passwordFile}'", passwordFile);

        var password = File.ReadAllText(passwordFile).Trim();
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException($"Redis password file is empty: '{passwordFile}'");

        // Добавляем пароль в строку подключения StackExchange.Redis
        return $"{cs},password={password}";
    }

    /// <summary>
    /// Формирует строку подключения к PostgreSQL с поддержкой чтения пароля из файла.
    /// </summary>
    /// <exception cref="InvalidOperationException">Если строка подключения не указана или файл пароля пуст.</exception>
    /// <exception cref="FileNotFoundException">Если файл пароля не найден.</exception>
    private static string BuildPostgresConnectionString(IConfiguration config)
    {
        var cs = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("Missing connection string: ConnectionStrings:DefaultConnection");

        var csb = new NpgsqlConnectionStringBuilder(cs);

        if (!string.IsNullOrWhiteSpace(csb.Password))
            return csb.ConnectionString;

        var passwordFile = config["Db:PasswordFile"];
        if (string.IsNullOrWhiteSpace(passwordFile))
            return csb.ConnectionString;

        if (!File.Exists(passwordFile))
            throw new FileNotFoundException($"Password file not found: '{passwordFile}'", passwordFile);

        var password = File.ReadAllText(passwordFile).Trim();
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException($"Password file is empty: '{passwordFile}'");

        csb.Password = password;
        return csb.ConnectionString;
    }
}
