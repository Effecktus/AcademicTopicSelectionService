using Asp.Versioning;
using AcademicTopicSelectionService.API.RateLimiting;
using AcademicTopicSelectionService.Application.Auth;
using AcademicTopicSelectionService.Application.Dictionaries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// Аутентификация и управление сессиями (login, refresh, logout).
/// </summary>
/// <param name="authService">Сервис аутентификации.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    private const string RefreshCookieName = "refreshToken";

    /// <summary>
    /// Войти в систему.
    /// </summary>
    /// <remarks>
    /// Возвращает access-токен в теле ответа.
    /// Refresh-токен устанавливается в <c>httpOnly</c>-cookie <c>refreshToken</c> и клиентом явно не читается.
    /// Access-токен передавать в заголовке <c>Authorization: Bearer &lt;token&gt;</c>.
    /// </remarks>
    [ProducesResponseType(typeof(AccessTokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting(RateLimitPolicyNames.AuthLogin)]
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AccessTokenDto>> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken ct = default)
    {
        var result = await authService.LoginAsync(request, ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                AuthError.InvalidCredentials or AuthError.UserInactive =>
                    Problem(title: "Unauthorized", detail: result.Message,
                        statusCode: StatusCodes.Status401Unauthorized),
                _ => Problem(title: "Bad Request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        var auth = result.Value!;
        SetRefreshCookie(auth.RefreshToken, auth.RefreshTokenExpiresAt);
        return Ok(ToDto(auth));
    }

    /// <summary>
    /// Обновить access-токен по refresh-токену из httpOnly-cookie.
    /// </summary>
    /// <remarks>
    /// Стратегия ротации: старый refresh-токен отзывается, в cookie устанавливается новый.
    /// Тело запроса не требуется.
    /// </remarks>
    [ProducesResponseType(typeof(AccessTokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting(RateLimitPolicyNames.AuthRefresh)]
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AccessTokenDto>> RefreshAsync(CancellationToken ct = default)
    {
        var token = Request.Cookies[RefreshCookieName];
        if (string.IsNullOrWhiteSpace(token))
        {
            return Problem(title: "Unauthorized", detail: "Refresh token cookie is missing.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await authService.RefreshAsync(new RefreshTokenRequest(token), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                AuthError.InvalidToken or AuthError.UserInactive =>
                    Problem(title: "Unauthorized", detail: result.Message,
                        statusCode: StatusCodes.Status401Unauthorized),
                _ => Problem(title: "Bad Request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        var auth = result.Value!;
        SetRefreshCookie(auth.RefreshToken, auth.RefreshTokenExpiresAt);
        return Ok(ToDto(auth));
    }

    /// <summary>
    /// Выйти из системы (отозвать refresh-токен из cookie).
    /// </summary>
    /// <remarks>
    /// Если cookie отсутствует — возвращает 204 (идемпотентный выход).
    /// </remarks>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync(CancellationToken ct = default)
    {
        var token = Request.Cookies[RefreshCookieName];
        if (string.IsNullOrWhiteSpace(token))
            return NoContent();

        var result = await authService.LogoutAsync(new RefreshTokenRequest(token), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                AuthError.InvalidToken =>
                    Problem(title: "Unauthorized", detail: result.Message,
                        statusCode: StatusCodes.Status401Unauthorized),
                _ => Problem(title: "Bad Request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        DeleteRefreshCookie();
        return NoContent();
    }

    // ------------------------------------------------------------------

    private static AccessTokenDto ToDto(AuthResponse r) => new(r.AccessToken, r.UserId, r.Email, r.Role);

    private void SetRefreshCookie(string token, DateTime expiresAt) =>
        Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt,
            Path = "/"
        });

    private void DeleteRefreshCookie() =>
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });
}
