namespace AcademicTopicSelectionService.API.RateLimiting;

/// <summary>
/// Имена политик ограничения частоты запросов.
/// </summary>
public static class RateLimitPolicyNames
{
    public const string AuthLogin = "auth-login";
    public const string AuthRefresh = "auth-refresh";
}
