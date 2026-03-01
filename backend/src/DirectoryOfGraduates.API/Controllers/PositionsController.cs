using Asp.Versioning;
using DirectoryOfGraduates.Application.Dictionaries;
using DirectoryOfGraduates.Application.Dictionaries.Positions;
using Microsoft.AspNetCore.Mvc;

namespace DirectoryOfGraduates.API.Controllers;

/// <summary>
/// CRUD для справочника должностей преподавателей (<c>Positions</c>).
/// </summary>
/// <param name="service">Сервис по работе с базой данных.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/positions")]
[Produces("application/json")]
public sealed class PositionsController(IPositionsService service) : ControllerBase
{
    /// <summary>
    /// Получить список должностей.
    /// </summary>
    /// <param name="searchString">Поиск по <c>Name</c> / <c>DisplayName</c>.</param>
    /// <param name="page">Номер страницы (>= 1).</param>
    /// <param name="pageSize">Размер страницы (1..200).</param>
    /// <param name="ct">Токен отмены.</param>
    [ProducesResponseType(typeof(PagedResult<PositionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [HttpGet]
    public async Task<ActionResult<PagedResult<PositionDto>>> ListAsync(
        [FromQuery] string? searchString,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await service.ListAsync(new ListPositionsQuery(searchString, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Получить должность по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор должности.</param>
    /// <param name="ct">Токен отмены.</param>
    [ProducesResponseType(typeof(PositionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PositionDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var position = await service.GetAsync(id, ct);
        return position is null
            ? Problem(title: "Not Found", detail: "Position not found", statusCode: StatusCodes.Status404NotFound,
                instance: id.ToString())
            : Ok(position);
    }

    /// <summary>
    /// Создать должность.
    /// </summary>
    [ProducesResponseType(typeof(PositionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [HttpPost]
    public async Task<ActionResult<PositionDto>> CreateAsync(
        [FromBody] UpsertNamedItemRequest body,
        CancellationToken ct = default)
    {
        var result = await service.CreateAsync(new UpsertPositionCommand(body.Name, body.DisplayName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                PositionsError.Validation => Problem(title: "Validation error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                PositionsError.Conflict => Problem(title: "Conflict", detail: result.Message,
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
    /// Полностью обновить должность (PUT). Все поля обязательны.
    /// </summary>
    [ProducesResponseType(typeof(PositionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PositionDto>> UpdateAsync(
        Guid id,
        [FromBody] UpsertNamedItemRequest body,
        CancellationToken ct = default)
    {
        var result = await service.UpdateAsync(id, new UpsertPositionCommand(body.Name, body.DisplayName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                PositionsError.NotFound => Problem(title: "Not Found", detail: result.Message,
                    statusCode: StatusCodes.Status404NotFound, instance: id.ToString()),
                PositionsError.Validation => Problem(title: "Validation error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                PositionsError.Conflict => Problem(title: "Conflict", detail: result.Message,
                    statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad request", detail: result.Message, statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Частично обновить должность (PATCH). Обновляются только переданные поля.
    /// </summary>
    [ProducesResponseType(typeof(PositionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<PositionDto>> PatchAsync(
        Guid id,
        [FromBody] PatchNamedItemRequest body,
        CancellationToken ct = default)
    {
        var result = await service.PatchAsync(id, new UpsertPositionCommand(body.Name, body.DisplayName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                PositionsError.NotFound => Problem(title: "Not Found", detail: result.Message,
                    statusCode: StatusCodes.Status404NotFound, instance: id.ToString()),
                PositionsError.Validation => Problem(title: "Validation error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                PositionsError.Conflict => Problem(title: "Conflict", detail: result.Message,
                    statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad request", detail: result.Message, statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Удалить должность.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted
            ? NoContent()
            : Problem(title: "Not Found", detail: "Position not found", statusCode: StatusCodes.Status404NotFound,
                instance: id.ToString());
    }
}
