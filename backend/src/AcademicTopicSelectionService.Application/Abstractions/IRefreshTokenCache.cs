namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Кэш refresh-токенов. Реализуется через Redis.
/// Ключ: значение токена → значение: <c>userId</c>, TTL = время жизни токена.
/// </summary>
public interface IRefreshTokenCache
{
    /// <summary>
    /// Сохраняет refresh-токен с указанным временем жизни.
    /// </summary>
    /// <param name="token">Значение refresh-токена (случайная строка).</param>
    /// <param name="userId">Идентификатор владельца токена.</param>
    /// <param name="expiry">Время жизни ключа в Redis.</param>
    /// <param name="ct">Токен отмены.</param>
    Task StoreAsync(string token, Guid userId, TimeSpan expiry, CancellationToken ct);

    /// <summary>
    /// Возвращает идентификатор пользователя по значению активного токена.
    /// </summary>
    /// <param name="token">Значение refresh-токена.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>userId или <c>null</c>, если токен не найден / истёк.</returns>
    Task<Guid?> GetUserIdAsync(string token, CancellationToken ct);

    /// <summary>
    /// Удаляет refresh-токен (logout / ротация).
    /// </summary>
    Task RemoveAsync(string token, CancellationToken ct);
}
