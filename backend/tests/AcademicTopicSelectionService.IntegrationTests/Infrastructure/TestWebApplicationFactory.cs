using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AcademicTopicSelectionService.IntegrationTests.Infrastructure;

/// <summary>
/// Фабрика тестового приложения. Подменяет строку подключения к БД через конфигурацию,
/// не трогая DI-регистрации, чтобы не нарушать внутреннюю инициализацию EF Core.
/// </summary>
public sealed class TestWebApplicationFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString
            });
        });

        builder.UseEnvironment("Testing");
    }
}
