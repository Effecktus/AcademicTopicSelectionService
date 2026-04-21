using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AcademicTopicSelectionService.Application.Auth;

/// <summary>
/// Запрос на вход в систему.
/// </summary>
/// <param name="Email">Email пользователя.</param>
/// <param name="Password">Пароль в открытом виде.</param>
public sealed record LoginRequest(
    [Required] string Email,
    [Required] string Password);

/// <summary>
/// Запрос на создание учётной записи (только администратором API).
/// </summary>
/// <param name="Email">Email (уникальный).</param>
/// <param name="Password">Пароль (политика длины и состава — см. <c>CredentialValidation</c>).</param>
/// <param name="FirstName">Имя.</param>
/// <param name="LastName">Фамилия.</param>
/// <param name="MiddleName">Отчество (опционально).</param>
/// <param name="RoleId">Идентификатор роли из справочника <c>UserRoles</c>.</param>
public sealed record CreateUserRequest(
    [Required] string Email,
    [Required] string Password,
    [Required] string FirstName,
    [Required] string LastName,
    string? MiddleName,
    [Required] Guid RoleId);

/// <summary>
/// Ответ после создания пользователя (без выдачи токенов — вход через <c>/auth/login</c>).
/// </summary>
/// <param name="UserId">Идентификатор созданного пользователя.</param>
/// <param name="Email">Email (нормализованный).</param>
/// <param name="Role">Системное имя роли (<c>CodeName</c>).</param>
public sealed record CreatedUserDto(Guid UserId, string Email, string Role);

/// <summary>
/// Запрос на обновление access-токена или выход из системы.
/// </summary>
/// <param name="RefreshToken">Значение refresh-токена.</param>
public sealed record RefreshTokenRequest(
    [property: JsonPropertyName("refreshToken")]
    [Required] string RefreshToken);

/// <summary>
/// Внутренний результат выдачи токенов (используется контроллером для установки cookie и формирования ответа).
/// </summary>
/// <param name="AccessToken">JWT access-токен.</param>
/// <param name="RefreshToken">Refresh-токен (устанавливается контроллером в httpOnly-cookie, не передаётся в теле ответа).</param>
/// <param name="RefreshTokenExpiresAt">Время истечения refresh-токена (UTC) — для установки срока жизни cookie.</param>
/// <param name="UserId">Идентификатор пользователя.</param>
/// <param name="Email">Email пользователя.</param>
/// <param name="Role">Системное имя роли (например, <c>Student</c>).</param>
public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    Guid UserId,
    string Email,
    string Role);

/// <summary>
/// Публичный ответ API после успешного входа или обновления токена.
/// Refresh-токен клиенту не возвращается — он устанавливается в httpOnly-cookie.
/// </summary>
/// <param name="AccessToken">JWT access-токен. Передавать в заголовке <c>Authorization: Bearer &lt;token&gt;</c>.</param>
/// <param name="UserId">Идентификатор пользователя.</param>
/// <param name="Email">Email пользователя.</param>
/// <param name="Role">Системное имя роли (например, <c>Student</c>).</param>
public sealed record AccessTokenDto(
    string AccessToken,
    Guid UserId,
    string Email,
    string Role);

/// <summary>
/// Типы ошибок при операциях аутентификации.
/// </summary>
public enum AuthError
{
    /// <summary>Ошибка валидации входных данных.</summary>
    Validation,

    /// <summary>Неверный email или пароль.</summary>
    InvalidCredentials,

    /// <summary>Пользователь с таким email уже существует.</summary>
    EmailAlreadyExists,

    /// <summary>Refresh-токен недействителен или истёк.</summary>
    InvalidToken,

    /// <summary>Аккаунт пользователя деактивирован.</summary>
    UserInactive
}
