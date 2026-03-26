using Asp.Versioning;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.UserRoles;
using Microsoft.AspNetCore.Mvc;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// CRUD для справочника ролей пользователей (<c>UserRoles</c>).
/// </summary>
/// <param name="service">Сервис по работе с базой данных.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/user-roles")]
[Produces("application/json")]
public sealed class UserRolesController(IUserRolesService service) : ControllerBase
{
    /// <summary>
    /// Получить список ролей пользователей.
    /// </summary>
    /// <param name="searchString">Поиск по <c>Name</c> / <c>DisplayName</c>.</param>
    /// <param name="page">Номер страницы (>= 1).</param>
    /// <param name="pageSize">Размер страницы (1..200).</param>
    /// <param name="ct">Токен отмены.</param>
    [ProducesResponseType(typeof(PagedResult<UserRoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserRoleDto>>> ListAsync(
        [FromQuery] string? searchString,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await service.ListAsync(new ListUserRolesQuery(searchString, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Получить роль по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор роли.</param>
    /// <param name="ct">Токен отмены.</param>
    [ProducesResponseType(typeof(UserRoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserRoleDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var role = await service.GetAsync(id, ct);
        return role is null
            ? Problem(title: "Not Found", detail: "UserRole not found", statusCode: StatusCodes.Status404NotFound,
                instance: id.ToString())
            : Ok(role);
    }

    /// <summary>
    /// Создать роль.
    /// </summary>
    [ProducesResponseType(typeof(UserRoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [HttpPost]
    public async Task<ActionResult<UserRoleDto>> CreateAsync(
        [FromBody] UpsertNamedItemRequest body,
        CancellationToken ct = default)
    {
        var result = await service.CreateAsync(new UpsertUserRoleCommand(body.CodeName, body.DisplayName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                UserRolesError.Validation => Problem(title: "Validation error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                UserRolesError.Conflict => Problem(title: "Conflict", detail: result.Message,
                    statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad request", detail: result.Message, statusCode: StatusCodes.Status400BadRequest)
            };
        }

        var routeVersion = RouteData.Values["version"]?.ToString();
        return routeVersion is null
            ? CreatedAtAction(nameof(GetAsync), new { id = result.Value!.Id }, result.Value)
            : CreatedAtAction(nameof(GetAsync), new { id = result.Value!.Id, version = routeVersion }, result.Value);
    }

    /// <summary>
    /// Полностью обновить роль (PUT). Все поля обязательны.
    /// </summary>
    [ProducesResponseType(typeof(UserRoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserRoleDto>> UpdateAsync(
        Guid id,
        [FromBody] UpsertNamedItemRequest body,
        CancellationToken ct = default)
    {
        var result = await service.UpdateAsync(id, new UpsertUserRoleCommand(body.CodeName, body.DisplayName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                UserRolesError.NotFound => Problem(title: "Not Found", detail: result.Message,
                    statusCode: StatusCodes.Status404NotFound, instance: id.ToString()),
                UserRolesError.Validation => Problem(title: "Validation error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                UserRolesError.Conflict => Problem(title: "Conflict", detail: result.Message,
                    statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad request", detail: result.Message, statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Частично обновить роль (PATCH). Обновляются только переданные поля.
    /// </summary>
    [ProducesResponseType(typeof(UserRoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<UserRoleDto>> PatchAsync(
        Guid id,
        [FromBody] PatchNamedItemRequest body,
        CancellationToken ct = default)
    {
        var result = await service.PatchAsync(id, new UpsertUserRoleCommand(body.CodeName, body.DisplayName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                UserRolesError.NotFound => Problem(title: "Not Found", detail: result.Message,
                    statusCode: StatusCodes.Status404NotFound, instance: id.ToString()),
                UserRolesError.Validation => Problem(title: "Validation error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                UserRolesError.Conflict => Problem(title: "Conflict", detail: result.Message,
                    statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad request", detail: result.Message, statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Удалить роль.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted
            ? NoContent()
            : Problem(title: "Not Found", detail: "UserRole not found", statusCode: StatusCodes.Status404NotFound,
                instance: id.ToString());
    }
}
