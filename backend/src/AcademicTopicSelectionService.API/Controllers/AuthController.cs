using Asp.Versioning;
using AcademicTopicSelectionService.Application.Auth;
using AcademicTopicSelectionService.Application.Dictionaries;
using Microsoft.AspNetCore.Mvc;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// Аутентификация и управление сессиями (login, register, refresh, logout).
/// </summary>
/// <param name="authService">Сервис аутентификации.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>
    /// Войти в систему.
    /// </summary>
    /// <remarks>
    /// Возвращает пару access/refresh токенов.
    /// Access-токен передавать в заголовке <c>Authorization: Bearer &lt;token&gt;</c>.
    /// </remarks>
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> LoginAsync(
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

        return Ok(result.Value);
    }

    /// <summary>
    /// Зарегистрировать нового пользователя.
    /// </summary>
    /// <remarks>
    /// Сразу выдаёт токены — пользователь считается вошедшим после регистрации.
    /// </remarks>
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> RegisterAsync(
        [FromBody] RegisterRequest request,
        CancellationToken ct = default)
    {
        var result = await authService.RegisterAsync(request, ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                AuthError.EmailAlreadyExists =>
                    Problem(title: "Conflict", detail: result.Message,
                        statusCode: StatusCodes.Status409Conflict),
                AuthError.Validation =>
                    Problem(title: "Validation Error", detail: result.Message,
                        statusCode: StatusCodes.Status400BadRequest),
                _ => Problem(title: "Bad Request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>
    /// Обновить access-токен по refresh-токену.
    /// </summary>
    /// <remarks>
    /// Стратегия ротации: старый refresh-токен отзывается, выдаётся новая пара.
    /// </remarks>
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> RefreshAsync(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct = default)
    {
        var result = await authService.RefreshAsync(request, ct);
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

        return Ok(result.Value);
    }

    /// <summary>
    /// Выйти из системы (отозвать refresh-токен).
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct = default)
    {
        var result = await authService.LogoutAsync(request, ct);
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

        return NoContent();
    }
}
