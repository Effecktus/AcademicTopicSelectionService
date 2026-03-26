using AcademicTopicSelectionService.Domain.Entities;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Абстракция для генерации JWT access-токенов и refresh-значений.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Генерирует подписанный JWT access-токен для пользователя.
    /// Включает claims: sub, email, role, jti.
    /// </summary>
    /// <param name="user">Пользователь (должна быть загружена навигация <c>Role</c>).</param>
    /// <returns>Строка JWT.</returns>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Генерирует криптографически случайное значение для refresh-токена.
    /// </summary>
    /// <returns>Base64-строка длиной 64 байта.</returns>
    string GenerateRefreshTokenValue();

    /// <summary>
    /// Возвращает дату истечения refresh-токена (UTC) согласно конфигурации.
    /// </summary>
    DateTime GetRefreshTokenExpiration();
}
