using AcademicTopicSelectionService.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AcademicTopicSelectionService.Infrastructure.Data;

/// <summary>
/// Подготовка базы данных при старте приложения: применение миграций и установка пароля администратора.
/// </summary>
/// <remarks>
/// Миграции применяются только при <c>Db:ApplyMigrationsOnStartup = true</c> (включено в Development).
/// В Production миграции применяются отдельным шагом развёртывания: несколько реплик не должны мигрировать одновременно.
/// </remarks>
public static class DatabaseInitializer
{
    /// <summary>
    /// Применяет миграции (если включено) и задаёт пароль администратора из конфигурации.
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (configuration.GetValue<bool>("Db:ApplyMigrationsOnStartup"))
        {
            logger.LogInformation("Применение миграций базы данных");
            await db.Database.MigrateAsync(ct);
        }

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await EnsureAdminPasswordAsync(db, hasher, configuration, logger, ct);
    }

    /// <summary>
    /// Устанавливает пароль администратору, созданному миграцией с <see cref="SystemSeed.AdminPasswordPlaceholder"/>.
    /// Уже установленный пароль не перезаписывается: его можно сменить через приложение.
    /// </summary>
    private static async Task EnsureAdminPasswordAsync(
        ApplicationDbContext db,
        IPasswordHasher hasher,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken ct)
    {
        var password = DependencyInjection.ReadSecretValue(configuration["Admin:Password"], configuration["Admin:PasswordFile"]);
        if (password is null)
        {
            logger.LogWarning(
                "Admin:Password и Admin:PasswordFile не заданы: пароль администратора не устанавливается");
            return;
        }

        var admin = await db.Users.FirstOrDefaultAsync(u => u.Id == SystemSeed.Users.Admin, ct);
        if (admin is null)
        {
            logger.LogWarning("Учётная запись администратора {AdminId} не найдена", SystemSeed.Users.Admin);
            return;
        }

        if (admin.PasswordHash != SystemSeed.AdminPasswordPlaceholder)
            return;

        admin.PasswordHash = hasher.Hash(password);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Пароль администратора {Email} установлен из конфигурации", admin.Email);
    }
}
