using Asp.Versioning;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.Application.Auth;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// Управление учётными записями пользователей (только администратор).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Produces("application/json")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class UsersController(IUserAccountsService userAccountsService) : ControllerBase
{
    /// <summary>
    /// Список пользователей с пагинацией и опциональными фильтрами.
    /// </summary>
    [ProducesResponseType(typeof(PagedResult<UserListItemDto>), StatusCodes.Status200OK)]
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserListItemDto>>> ListAsync(
        [FromQuery] Guid? roleId,
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await userAccountsService.ListAsync(new ListUsersQuery(page, pageSize, roleId, query), ct);
        return Ok(result);
    }

    /// <summary>
    /// Создать пользователя. Дальнейший вход — через <c>POST /auth/login</c>.
    /// </summary>
    [ProducesResponseType(typeof(CreatedUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [HttpPost]
    public async Task<ActionResult<CreatedUserDto>> CreateAsync(
        [FromBody] CreateUserRequest request,
        CancellationToken ct = default)
    {
        var result = await userAccountsService.CreateAsync(request, ct);
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

        return StatusCode(StatusCodes.Status201Created, result.Value!);
    }
}
