namespace AcademicTopicSelectionService.Infrastructure.Auth;

/// <summary>
/// Настройки подключения к Redis, читаются из секции <c>Redis</c> конфигурации.
/// </summary>
public sealed class RedisSettings
{
    /// <summary>Имя секции в appsettings.</summary>
    public const string SectionName = "Redis";

    /// <summary>
    /// Строка подключения StackExchange.Redis (например, <c>localhost:6379</c>
    /// или <c>redis:6379,password=secret</c>).
    /// </summary>
    public string ConnectionString { get; init; } = null!;

    /// <summary>
    /// Путь к файлу с паролем Redis (опционально, для Docker secrets).
    /// Если указан, пароль подставляется в строку подключения.
    /// </summary>
    public string? PasswordFile { get; init; }
}
