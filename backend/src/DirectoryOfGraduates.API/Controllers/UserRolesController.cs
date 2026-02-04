using Asp.Versioning;
using DirectoryOfGraduates.Application.Dictionaries.UserRoles;
using Microsoft.AspNetCore.Mvc;

namespace DirectoryOfGraduates.API.Controllers;

/// <summary>
/// CRUD для справочника ролей пользователей (<c>UserRoles</c>).
/// Эталонный контроллер: тонкий, без бизнес-логики — только HTTP + вызов сервиса Application.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/user-roles")]
[Produces("application/json")]
public sealed class UserRolesController : ControllerBase
{
    private readonly IUserRolesService _service;

    public UserRolesController(IUserRolesService service)
    {
        _service = service;
    }

    /// <summary>
    /// Получить список ролей пользователей.
    /// </summary>
    /// <param name="q">Поиск по <c>Name</c> / <c>DisplayName</c> (ILIKE).</param>
    /// <param name="page">Номер страницы (>= 1).</param>
    /// <param name="pageSize">Размер страницы (1..200).</param>
    /// <param name="ct">Токен отмены.</param>
    [ProducesResponseType(typeof(ListResponse<UserRoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [HttpGet]
    public async Task<ActionResult<ListResponse<UserRoleDto>>> ListAsync(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(new ListUserRolesQuery(q, page, pageSize), ct);
        return Ok(new ListResponse<UserRoleDto>(result.Page, result.PageSize, result.Total, result.Items));
    }

    /// <summary>
    /// Получить роль по идентификатору.
    /// </summary>
    [ProducesResponseType(typeof(UserRoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserRoleDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var role = await _service.GetByIdAsync(id, ct);
        return role is null
            ? NotFound(new { message = "UserRole not found", id })
            : Ok(role);
    }

    /// <summary>
    /// Создать роль.
    /// </summary>
    [ProducesResponseType(typeof(UserRoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [HttpPost]
    public async Task<ActionResult<UserRoleDto>> CreateAsync([FromBody] UpsertUserRoleRequest body, CancellationToken ct)
    {
        var result = await _service.CreateAsync(new UpsertUserRoleCommand(body.Name, body.DisplayName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                UserRolesError.Validation => Problem(title: "Validation error", detail: result.Message, statusCode: StatusCodes.Status400BadRequest),
                UserRolesError.Conflict => Problem(title: "Conflict", detail: result.Message, statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad request", detail: result.Message, statusCode: StatusCodes.Status400BadRequest)
            };
        }

        var routeVersion = RouteData.Values["version"]?.ToString();
        return routeVersion is null
            ? CreatedAtAction(nameof(GetByIdAsync), new { id = result.Value!.Id }, result.Value)
            : CreatedAtAction(nameof(GetByIdAsync), new { id = result.Value!.Id, version = routeVersion }, result.Value);
    }

    /// <summary>
    /// Полностью обновить роль (PUT). Все поля обязательны.
    /// </summary>
    [ProducesResponseType(typeof(UserRoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserRoleDto>> UpdateAsync(Guid id, [FromBody] UpsertUserRoleRequest body, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, new UpsertUserRoleCommand(body.Name, body.DisplayName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                UserRolesError.NotFound => NotFound(new { message = result.Message, id }),
                UserRolesError.Validation => Problem(title: "Validation error", detail: result.Message, statusCode: StatusCodes.Status400BadRequest),
                UserRolesError.Conflict => Problem(title: "Conflict", detail: result.Message, statusCode: StatusCodes.Status409Conflict),
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<UserRoleDto>> PatchAsync(Guid id, [FromBody] PatchUserRoleRequest body, CancellationToken ct)
    {
        var result = await _service.PatchAsync(id, new PatchUserRoleCommand(body.Name, body.DisplayName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                UserRolesError.NotFound => NotFound(new { message = result.Message, id }),
                UserRolesError.Validation => Problem(title: "Validation error", detail: result.Message, statusCode: StatusCodes.Status400BadRequest),
                UserRolesError.Conflict => Problem(title: "Conflict", detail: result.Message, statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad request", detail: result.Message, statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Удалить роль.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound(new { message = "UserRole not found", id });
    }

    /// <summary>
    /// Тело запроса для создания/полного обновления роли (POST/PUT).
    /// Оба поля обязательны.
    /// </summary>
    /// <param name="Name">Системное имя (например, <c>Student</c>).</param>
    /// <param name="DisplayName">Отображаемое имя (например, <c>Студент</c>).</param>
    public sealed record UpsertUserRoleRequest(string? Name, string? DisplayName);

    /// <summary>
    /// Тело запроса для частичного обновления роли (PATCH).
    /// Передавайте только поля, которые нужно изменить.
    /// </summary>
    /// <param name="Name">Системное имя (опционально).</param>
    /// <param name="DisplayName">Отображаемое имя (опционально).</param>
    public sealed record PatchUserRoleRequest(string? Name, string? DisplayName);

    /// <summary>
    /// Обёртка для постраничного ответа API.
    /// </summary>
    /// <typeparam name="T">Тип элементов списка.</typeparam>
    /// <param name="Page">Текущий номер страницы.</param>
    /// <param name="PageSize">Количество элементов на странице.</param>
    /// <param name="Total">Общее количество элементов.</param>
    /// <param name="Items">Элементы текущей страницы.</param>
    public sealed record ListResponse<T>(int Page, int PageSize, long Total, IReadOnlyList<T> Items);
}

