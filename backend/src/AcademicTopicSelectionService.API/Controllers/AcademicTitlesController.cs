using Asp.Versioning;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.AcademicTitles;
using Microsoft.AspNetCore.Mvc;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// CRUD для справочника учёных званий (<c>AcademicTitles</c>).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/academic-titles")]
[Produces("application/json")]
public sealed class AcademicTitlesController(IAcademicTitlesService service) : ControllerBase
{
    /// <summary>
    /// Получить список учёных званий.
    /// </summary>
    [ProducesResponseType(typeof(PagedResult<AcademicTitleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [HttpGet]
    public async Task<ActionResult<PagedResult<AcademicTitleDto>>> ListAsync(
        [FromQuery] string? searchString,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await service.ListAsync(new ListAcademicTitlesQuery(searchString, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Получить учёное звание по идентификатору.
    /// </summary>
    [ProducesResponseType(typeof(AcademicTitleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AcademicTitleDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var title = await service.GetAsync(id, ct);
        return title is null
            ? Problem(title: "Not Found", detail: "AcademicTitle not found",
                statusCode: StatusCodes.Status404NotFound, instance: id.ToString())
            : Ok(title);
    }

    /// <summary>
    /// Создать учёное звание.
    /// </summary>
    [ProducesResponseType(typeof(AcademicTitleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [HttpPost]
    public async Task<ActionResult<AcademicTitleDto>> CreateAsync(
        [FromBody] UpsertNamedItemRequest body,
        CancellationToken ct = default)
    {
        var result = await service.CreateAsync(new UpsertAcademicTitleCommand(body.Name, body.DisplayName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                AcademicTitlesError.Validation => Problem(title: "Validation error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                AcademicTitlesError.Conflict => Problem(title: "Conflict", detail: result.Message,
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
    /// Полностью обновить учёное звание (PUT). Все поля обязательны.
    /// </summary>
    [ProducesResponseType(typeof(AcademicTitleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AcademicTitleDto>> UpdateAsync(
        Guid id,
        [FromBody] UpsertNamedItemRequest body,
        CancellationToken ct = default)
    {
        var result = await service.UpdateAsync(id, new UpsertAcademicTitleCommand(body.Name, body.DisplayName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                AcademicTitlesError.NotFound => Problem(title: "Not Found", detail: result.Message,
                    statusCode: StatusCodes.Status404NotFound, instance: id.ToString()),
                AcademicTitlesError.Validation => Problem(title: "Validation error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                AcademicTitlesError.Conflict => Problem(title: "Conflict", detail: result.Message,
                    statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad request", detail: result.Message, statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Частично обновить учёное звание (PATCH). Обновляются только переданные поля.
    /// </summary>
    [ProducesResponseType(typeof(AcademicTitleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<AcademicTitleDto>> PatchAsync(
        Guid id,
        [FromBody] PatchNamedItemRequest body,
        CancellationToken ct = default)
    {
        var result = await service.PatchAsync(id, new UpsertAcademicTitleCommand(body.Name, body.DisplayName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                AcademicTitlesError.NotFound => Problem(title: "Not Found", detail: result.Message,
                    statusCode: StatusCodes.Status404NotFound, instance: id.ToString()),
                AcademicTitlesError.Validation => Problem(title: "Validation error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                AcademicTitlesError.Conflict => Problem(title: "Conflict", detail: result.Message,
                    statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad request", detail: result.Message, statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Удалить учёное звание.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted
            ? NoContent()
            : Problem(title: "Not Found", detail: "AcademicTitle not found",
                statusCode: StatusCodes.Status404NotFound, instance: id.ToString());
    }
}
