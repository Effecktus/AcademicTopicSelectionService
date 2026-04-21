using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Security;
using AcademicTopicSelectionService.Domain.Entities;

namespace AcademicTopicSelectionService.Application.Auth;

/// <inheritdoc />
public sealed class AuthService(
    IUsersRepository usersRepo,
    IRefreshTokenCache refreshTokenCache,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtGenerator) : IAuthService
{
    /// <inheritdoc />
    public async Task<Result<AuthResponse, AuthError>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        if (!CredentialValidation.TryNormalizeEmail(request.Email, out var email, out _))
            return Fail(AuthError.InvalidCredentials, "Invalid email or password");

        var user = await usersRepo.GetByEmailAsync(email, ct);
        if (user is null)
            return Fail(AuthError.InvalidCredentials, "Invalid email or password");

        if (!user.IsActive)
            return Fail(AuthError.UserInactive, "User account is deactivated");

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            return Fail(AuthError.InvalidCredentials, "Invalid email or password");

        return Result<AuthResponse, AuthError>.Ok(await IssueTokensAsync(user, ct));
    }

    /// <inheritdoc />
    public async Task<Result<AuthResponse, AuthError>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        // Быстрая проверка в Redis: token → userId
        var userId = await refreshTokenCache.GetUserIdAsync(request.RefreshToken, ct);
        if (userId is null)
            return Fail(AuthError.InvalidToken, "Invalid or expired refresh token");

        // Загружаем пользователя из БД (нужны role, isActive)
        var user = await usersRepo.GetByIdAsync(userId.Value, ct);
        if (user is null)
            return Fail(AuthError.InvalidToken, "User not found");

        if (!user.IsActive)
            return Fail(AuthError.UserInactive, "User account is deactivated");

        // Ротация: старый токен удаляется, выдаётся новая пара
        await refreshTokenCache.RemoveAsync(request.RefreshToken, ct);

        return Result<AuthResponse, AuthError>.Ok(await IssueTokensAsync(user, ct));
    }

    /// <inheritdoc />
    public async Task<Result<bool, AuthError>> LogoutAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        var userId = await refreshTokenCache.GetUserIdAsync(request.RefreshToken, ct);
        if (userId is null)
            return Result<bool, AuthError>.Fail(AuthError.InvalidToken, "Invalid or expired refresh token");

        await refreshTokenCache.RemoveAsync(request.RefreshToken, ct);
        return Result<bool, AuthError>.Ok(true);
    }

    // ------------------------------------------------------------------

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var accessToken = jwtGenerator.GenerateAccessToken(user);
        var refreshValue = jwtGenerator.GenerateRefreshTokenValue();
        var expiresAt = jwtGenerator.GetRefreshTokenExpiration();

        // Сохраняем в Redis с TTL
        var ttl = expiresAt - DateTime.UtcNow;
        await refreshTokenCache.StoreAsync(refreshValue, user.Id, ttl, ct);

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshValue,
            RefreshTokenExpiresAt: expiresAt,
            UserId: user.Id,
            Email: user.Email,
            Role: user.Role.CodeName);
    }

    private static Result<AuthResponse, AuthError> Fail(AuthError error, string message) =>
        Result<AuthResponse, AuthError>.Fail(error, message);
}
