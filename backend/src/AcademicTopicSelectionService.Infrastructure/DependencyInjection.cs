using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StackExchange.Redis;
using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Infrastructure.Auth;
using AcademicTopicSelectionService.Infrastructure.Data;
using AcademicTopicSelectionService.Infrastructure.Email;
using AcademicTopicSelectionService.Infrastructure.Repositories;
using AcademicTopicSelectionService.Infrastructure.Storage;
using Microsoft.Extensions.Options;

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

        // Репозитории бизнес-сущностей
        services.AddScoped<IApplicationActionsRepository, ApplicationActionsRepository>();

        // Репозитории справочников
        services.AddScoped<IUserRolesRepository, UserRolesRepository>();
        services.AddScoped<IApplicationStatusesRepository, ApplicationStatusesRepository>();
        services.AddScoped<IApplicationActionStatusesRepository, ApplicationActionStatusesRepository>();
        services.AddScoped<ITopicStatusesRepository, TopicStatusesRepository>();
        services.AddScoped<ITopicCreatorTypesRepository, TopicCreatorTypesRepository>();
        services.AddScoped<INotificationTypesRepository, NotificationTypesRepository>();
        services.AddScoped<IAcademicDegreesRepository, AcademicDegreesRepository>();
        services.AddScoped<IAcademicTitlesRepository, AcademicTitlesRepository>();
        services.AddScoped<IPositionsRepository, PositionsRepository>();
        services.AddScoped<IStudyGroupsRepository, StudyGroupsRepository>();
        services.AddScoped<ITeachersRepository, TeachersRepository>();
        services.AddScoped<ITopicsRepository, TopicsRepository>();
        services.AddScoped<IStudentsRepository, StudentsRepository>();
        services.AddScoped<IStudentApplicationsRepository, StudentApplicationsRepository>();
        services.AddScoped<ISupervisorRequestsRepository, SupervisorRequestsRepository>();
        services.AddScoped<INotificationsRepository, NotificationsRepository>();
        services.AddScoped<IGraduateWorksRepository, GraduateWorksRepository>();

        services.Configure<S3Options>(configuration.GetSection(S3Options.SectionName));
        RegisterFileStorage(services, configuration);

        // Email notifications
        services.Configure<EmailDispatchOptions>(configuration.GetSection(EmailDispatchOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddSingleton<IEmailTaskChannel, EmailTaskChannel>();
        services.AddScoped<LogEmailSender>();
        services.AddScoped<SmtpEmailSender>();
        services.AddScoped<IEmailSender>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<EmailDispatchOptions>>().Value;
            if (opts.Provider.Equals("Log", StringComparison.OrdinalIgnoreCase))
                return sp.GetRequiredService<LogEmailSender>();

            return sp.GetRequiredService<SmtpEmailSender>();
        });
        services.AddHostedService<EmailBackgroundService>();

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

    private static void RegisterFileStorage(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(S3Options.SectionName);
        var provider = section["Provider"] ?? "Development";

        if (!provider.Equals("S3", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IFileStorageService, DevelopmentFileStorageService>();
            return;
        }

        var options = BuildS3Options(configuration);
        services.AddSingleton(options);
        services.AddSingleton<IOptions<S3Options>>(_ => Options.Create(options));
        services.AddSingleton<IAmazonS3>(_ => CreateS3Client(options));
        services.AddSingleton<PresignAmazonS3>(_ =>
        {
            var presignClient = string.IsNullOrWhiteSpace(options.PublicEndpoint)
                ? CreateS3Client(options)
                : S3FileStorageService.CreatePresignClient(options);
            return new PresignAmazonS3(presignClient);
        });
        services.AddScoped<IFileStorageService, S3FileStorageService>();
    }

    private static S3Options BuildS3Options(IConfiguration configuration)
    {
        var options = configuration.GetSection(S3Options.SectionName).Get<S3Options>() ?? new S3Options();

        options.AccessKey = ReadSecretValue(options.AccessKey, options.AccessKeyFile);
        options.SecretKey = ReadSecretValue(options.SecretKey, options.SecretKeyFile);

        if (string.IsNullOrWhiteSpace(options.BucketName))
            throw new InvalidOperationException("Missing S3 bucket name: S3:BucketName");

        if (string.IsNullOrWhiteSpace(options.AccessKey))
            throw new InvalidOperationException("Missing S3 access key: S3:AccessKey or S3:AccessKeyFile");

        if (string.IsNullOrWhiteSpace(options.SecretKey))
            throw new InvalidOperationException("Missing S3 secret key: S3:SecretKey or S3:SecretKeyFile");

        if (string.IsNullOrWhiteSpace(options.Region))
            options.Region = "us-east-1";

        return options;
    }

    private static IAmazonS3 CreateS3Client(S3Options options)
    {
        var config = new AmazonS3Config
        {
            ForcePathStyle = options.ForcePathStyle
        };

        if (string.IsNullOrWhiteSpace(options.Endpoint))
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
        else
            config.ServiceURL = options.Endpoint;

        return new AmazonS3Client(
            new BasicAWSCredentials(options.AccessKey!, options.SecretKey!),
            config);
    }

    private static string? ReadSecretValue(string? inlineValue, string? filePath)
    {
        if (!string.IsNullOrWhiteSpace(inlineValue))
            return inlineValue.Trim();

        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Secret file not found: '{filePath}'", filePath);

        var value = File.ReadAllText(filePath).Trim();
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Secret file is empty: '{filePath}'");

        return value;
    }
}
