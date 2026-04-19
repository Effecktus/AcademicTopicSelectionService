using System.Security.Cryptography;
using System.Text;
using AcademicTopicSelectionService.API.Authorization;

namespace AcademicTopicSelectionService.API.Health;

/// <summary>
/// Доступ к <c>/health/db</c>: JWT с ролью Admin или заголовок <c>X-Health-Probe-Key</c>, если ключ задан в конфигурации.
/// </summary>
public static class HealthDbAccess
{
    private const string ProbeHeaderName = "X-Health-Probe-Key";

    /// <summary>
    /// Возвращает <c>true</c>, если запросу разрешена проверка БД.
    /// </summary>
    public static bool IsAuthorized(HttpContext httpContext, IConfiguration configuration)
    {
        var expected = configuration["Health:DbProbeKey"];
        var probeConfigured = !string.IsNullOrWhiteSpace(expected);

        if (httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole(AppRoles.Admin))
            return true;

        if (!probeConfigured)
            return false;

        var supplied = httpContext.Request.Headers[ProbeHeaderName].FirstOrDefault();
        return ProbeKeyEquals(expected!.Trim(), supplied);
    }

    private static bool ProbeKeyEquals(string expected, string? supplied)
    {
        if (supplied is null)
            return false;

        var a = Encoding.UTF8.GetBytes(expected);
        var b = Encoding.UTF8.GetBytes(supplied);
        if (a.Length != b.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
