using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AcademicTopicSelectionService.IntegrationTests.Infrastructure;

/// <summary>
/// Фабрика тестового приложения. Подменяет конфигурацию БД, Redis и JWT через in-memory коллекцию,
/// не трогая DI-регистрации, чтобы не нарушать внутреннюю инициализацию EF Core.
/// </summary>
public sealed class TestWebApplicationFactory(
    string connectionString,
    string? redisConnectionString = null)
    : WebApplicationFactory<Program>
{
    // Тестовый секрет — достаточно длинный для HMAC-SHA256 (минимум 32 символа)
    public const string TestJwtSecret = "test-only-secret-key-must-be-at-least-32-chars!!";
    public const string TestJwtIssuer = "TestIssuer";
    public const string TestJwtAudience = "TestAudience";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                // JWT — без этих ключей Program.cs бросает исключение при старте
                ["Jwt:SecretKey"] = TestJwtSecret,
                ["Jwt:Issuer"] = TestJwtIssuer,
                ["Jwt:Audience"] = TestJwtAudience,
                ["Jwt:AccessTokenExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "30",
            };

            if (redisConnectionString is not null)
            {
                settings["Redis:ConnectionString"] = redisConnectionString;
                // Сбрасываем путь к файлу пароля, чтобы не пытался читать файл в тестах
                settings["Redis:PasswordFile"] = "";
            }

            config.AddInMemoryCollection(settings);
        });

        builder.UseEnvironment("Testing");
    }
}
