using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Auth;

/// <summary>
/// Сервис аутентификации: вход, обновление и отзыв токенов.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Выполняет вход по email/паролю и возвращает пару access/refresh токенов.
    /// </summary>
    Task<Result<AuthResponse, AuthError>> LoginAsync(LoginRequest request, CancellationToken ct);

    /// <summary>
    /// Обновляет access-токен по действующему refresh-токену (ротация: старый токен отзывается).
    /// </summary>
    Task<Result<AuthResponse, AuthError>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct);

    /// <summary>
    /// Отзывает переданный refresh-токен (завершает сессию).
    /// </summary>
    Task<Result<bool, AuthError>> LogoutAsync(RefreshTokenRequest request, CancellationToken ct);
}
