using Asp.Versioning;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.ApplicationStatuses;
using Microsoft.AspNetCore.Mvc;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// CRUD для справочника статусов заявки (<c>ApplicationStatuses</c>).
/// </summary>
/// <param name="service">Сервис по работе с базой данных.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/application-statuses")]
[Produces("application/json")]
public sealed class ApplicationStatusesController(IApplicationStatusesService service) : ControllerBase
{
    /// <summary>
    /// Получить список статусов заявки.
    /// </summary>
    /// <param name="searchString">Поиск по <c>Name</c> / <c>DisplayName</c>.</param>
    /// <param name="page">Номер страницы (>= 1).</param>
    /// <param name="pageSize">Размер страницы (1..200).</param>
    /// <param name="ct">Токен отмены.</param>
    [ProducesResponseType(typeof(PagedResult<ApplicationStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [HttpGet]
    public async Task<ActionResult<PagedResult<ApplicationStatusDto>>> ListAsync(
        [FromQuery] string? searchString,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await service.ListAsync(new ListApplicationStatusQuery(searchString, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Получить статус заявки по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор статуса заявки.</param>
    /// <param name="ct">Токен отмены.</param>
    [ProducesResponseType(typeof(ApplicationStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApplicationStatusDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var status = await service.GetAsync(id, ct);
        return status is null
            ? Problem(title: "Not Found", detail: "ApplicationStatus not found",
                statusCode: StatusCodes.Status404NotFound, instance: id.ToString())
            : Ok(status);
    }

    /// <summary>
    /// Создать статус заявки.
    /// </summary>
    [ProducesResponseType(typeof(ApplicationStatusDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [HttpPost]
    public async Task<ActionResult<ApplicationStatusDto>> CreateAsync(
        [FromBody] UpsertNamedItemRequest body,
        CancellationToken ct = default)
    {
        var result = await service.CreateAsync(new UpsetApplicationStatusCommand(body.Name, body.DisplayName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                ApplicationStatusesError.Validation => Problem(title: "Validation error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                ApplicationStatusesError.Conflict => Problem(title: "Conflict", detail: result.Message,
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
    /// Полностью обновить статус заявки (PUT). Все поля обязательны.
    /// </summary>
    [ProducesResponseType(typeof(ApplicationStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApplicationStatusDto>> UpdateAsync(
        Guid id,
        [FromBody] UpsertNamedItemRequest body,
        CancellationToken ct = default)
    {
        var result = await service.UpdateAsync(id, new UpsetApplicationStatusCommand(body.Name, body.DisplayName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                ApplicationStatusesError.NotFound => Problem(title: "Not Found", detail: result.Message,
                    statusCode: StatusCodes.Status404NotFound, instance: id.ToString()),
                ApplicationStatusesError.Validation => Problem(title: "Validation error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                ApplicationStatusesError.Conflict => Problem(title: "Conflict", detail: result.Message,
                    statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad request", detail: result.Message, statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Частично обновить статус заявки (PATCH). Обновляются только переданные поля.
    /// </summary>
    [ProducesResponseType(typeof(ApplicationStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ApplicationStatusDto>> PatchAsync(
        Guid id,
        [FromBody] PatchNamedItemRequest body,
        CancellationToken ct = default)
    {
        var result = await service.PatchAsync(id, new UpsetApplicationStatusCommand(body.Name, body.DisplayName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                ApplicationStatusesError.NotFound => Problem(title: "Not Found", detail: result.Message,
                    statusCode: StatusCodes.Status404NotFound, instance: id.ToString()),
                ApplicationStatusesError.Validation => Problem(title: "Validation error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                ApplicationStatusesError.Conflict => Problem(title: "Conflict", detail: result.Message,
                    statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad request", detail: result.Message, statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Удалить статус заявки.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted
            ? NoContent()
            : Problem(title: "Not Found", detail: "ApplicationStatus not found",
                statusCode: StatusCodes.Status404NotFound, instance: id.ToString());
    }
}
