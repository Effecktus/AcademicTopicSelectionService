using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
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
        var email = NormalizeEmail(request.Email);

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
    public async Task<Result<AuthResponse, AuthError>> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);

        var validationError = ValidateRegister(request, email);
        if (validationError is not null)
            return Fail(AuthError.Validation, validationError);

        if (await usersRepo.ExistsByEmailAsync(email, ct))
            return Fail(AuthError.EmailAlreadyExists, "User with this email already exists");

        var user = new User
        {
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim(),
            RoleId = request.RoleId,
            IsActive = true
        };

        var created = await usersRepo.CreateAsync(user, ct);
        return Result<AuthResponse, AuthError>.Ok(await IssueTokensAsync(created, ct));
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
            UserId: user.Id,
            Email: user.Email,
            Role: user.Role.CodeName);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string? ValidateRegister(RegisterRequest req, string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "Email is required";
        if (!email.Contains('@'))
            return "Email is invalid";
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
            return "Password must be at least 6 characters";
        if (string.IsNullOrWhiteSpace(req.FirstName))
            return "FirstName is required";
        if (string.IsNullOrWhiteSpace(req.LastName))
            return "LastName is required";
        if (req.RoleId == Guid.Empty)
            return "RoleId is required";
        return null;
    }

    private static Result<AuthResponse, AuthError> Fail(AuthError error, string message) =>
        Result<AuthResponse, AuthError>.Fail(error, message);
}
