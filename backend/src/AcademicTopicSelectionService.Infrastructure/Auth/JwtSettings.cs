namespace AcademicTopicSelectionService.Infrastructure.Auth;

/// <summary>
/// Настройки JWT-аутентификации, читаются из секции <c>Jwt</c> конфигурации.
/// </summary>
public sealed class JwtSettings
{
    /// <summary>Имя секции в appsettings.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Секретный ключ подписи (минимум 32 символа, хранить в secrets/env).</summary>
    public string SecretKey { get; init; } = null!;

    /// <summary>Issuer токена.</summary>
    public string Issuer { get; init; } = null!;

    /// <summary>Audience токена.</summary>
    public string Audience { get; init; } = null!;

    /// <summary>Время жизни access-токена в минутах (по умолчанию 60).</summary>
    public int AccessTokenExpirationMinutes { get; init; } = 60;

    /// <summary>Время жизни refresh-токена в днях (по умолчанию 30).</summary>
    public int RefreshTokenExpirationDays { get; init; } = 30;
}
