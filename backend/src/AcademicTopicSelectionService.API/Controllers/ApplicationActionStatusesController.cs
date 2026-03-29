using Asp.Versioning;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.ApplicationActionStatuses;
using Microsoft.AspNetCore.Mvc;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// CRUD для справочника статусов действий по заявкам (<c>ApplicationActionStatuses</c>).
/// </summary>
/// <param name="service">Сервис по работе с базой данных.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/application-action-statuses")]
[Produces("application/json")]
public sealed class ApplicationActionStatusesController(IApplicationActionStatusesService service) : ControllerBase
{
    /// <summary>
    /// Получить список статусов действий по заявкам.
    /// </summary>
    /// <param name="searchString">Поиск по <c>CodeName</c> / <c>DisplayName</c>.</param>
    /// <param name="page">Номер страницы (>= 1).</param>
    /// <param name="pageSize">Размер страницы (1..200).</param>
    /// <param name="ct">Токен отмены.</param>
    [ProducesResponseType(typeof(PagedResult<ApplicationActionStatusDto>), StatusCodes.Status200OK)]
    [HttpGet]
    public async Task<ActionResult<PagedResult<ApplicationActionStatusDto>>> ListAsync(
        [FromQuery] string? searchString,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await service.ListAsync(
            new ListApplicationActionStatusQuery(searchString, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Получить статус действия по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор статуса действия.</param>
    /// <param name="ct">Токен отмены.</param>
    [ProducesResponseType(typeof(ApplicationActionStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApplicationActionStatusDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var status = await service.GetAsync(id, ct);
        return status is null
            ? Problem(title: "Not Found", detail: "ApplicationActionStatus not found",
                statusCode: StatusCodes.Status404NotFound, instance: id.ToString())
            : Ok(status);
    }

    /// <summary>
    /// Создать статус действия.
    /// </summary>
    [ProducesResponseType(typeof(ApplicationActionStatusDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [HttpPost]
    public async Task<ActionResult<ApplicationActionStatusDto>> CreateAsync(
        [FromBody] UpsertNamedItemRequest body,
        CancellationToken ct = default)
    {
        var result = await service.CreateAsync(
            new UpsertApplicationActionStatusCommand(body.CodeName, body.DisplayName), ct);

        if (result.Error is not null)
        {
            return result.Error switch
            {
                ApplicationActionStatusesError.Validation => Problem(title: "Validation error",
                    detail: result.Message, statusCode: StatusCodes.Status400BadRequest),
                ApplicationActionStatusesError.Conflict => Problem(title: "Conflict",
                    detail: result.Message, statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        var routeVersion = RouteData.Values["version"]?.ToString();
        return routeVersion is null
            ? CreatedAtAction(nameof(GetAsync), new { id = result.Value!.Id }, result.Value)
            : CreatedAtAction(nameof(GetAsync), new { id = result.Value!.Id, version = routeVersion }, result.Value);
    }

    /// <summary>
    /// Полностью обновить статус действия (PUT). Все поля обязательны.
    /// </summary>
    [ProducesResponseType(typeof(ApplicationActionStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApplicationActionStatusDto>> UpdateAsync(
        Guid id,
        [FromBody] UpsertNamedItemRequest body,
        CancellationToken ct = default)
    {
        var result = await service.UpdateAsync(id,
            new UpsertApplicationActionStatusCommand(body.CodeName, body.DisplayName), ct);

        if (result.Error is not null)
        {
            return result.Error switch
            {
                ApplicationActionStatusesError.NotFound => Problem(title: "Not Found",
                    detail: result.Message, statusCode: StatusCodes.Status404NotFound, instance: id.ToString()),
                ApplicationActionStatusesError.Validation => Problem(title: "Validation error",
                    detail: result.Message, statusCode: StatusCodes.Status400BadRequest),
                ApplicationActionStatusesError.Conflict => Problem(title: "Conflict",
                    detail: result.Message, statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Частично обновить статус действия (PATCH). Обновляются только переданные поля.
    /// </summary>
    [ProducesResponseType(typeof(ApplicationActionStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ApplicationActionStatusDto>> PatchAsync(
        Guid id,
        [FromBody] PatchNamedItemRequest body,
        CancellationToken ct = default)
    {
        var result = await service.PatchAsync(id,
            new UpsertApplicationActionStatusCommand(body.CodeName, body.DisplayName), ct);

        if (result.Error is not null)
        {
            return result.Error switch
            {
                ApplicationActionStatusesError.NotFound => Problem(title: "Not Found",
                    detail: result.Message, statusCode: StatusCodes.Status404NotFound, instance: id.ToString()),
                ApplicationActionStatusesError.Validation => Problem(title: "Validation error",
                    detail: result.Message, statusCode: StatusCodes.Status400BadRequest),
                ApplicationActionStatusesError.Conflict => Problem(title: "Conflict",
                    detail: result.Message, statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Удалить статус действия.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted
            ? NoContent()
            : Problem(title: "Not Found", detail: "ApplicationActionStatus not found",
                statusCode: StatusCodes.Status404NotFound, instance: id.ToString());
    }
}
