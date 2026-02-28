using Asp.Versioning;
using DirectoryOfGraduates.Application.Dictionaries;
using DirectoryOfGraduates.Application.Dictionaries.AcademicDegrees;
using Microsoft.AspNetCore.Mvc;

namespace DirectoryOfGraduates.API.Controllers;

/// <summary>
/// CRUD для справочника учёных степеней (<c>AcademicDegrees</c>).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/academic-degrees")]
[Produces("application/json")]
public sealed class AcademicDegreesController(IAcademicDegreesService service) : ControllerBase
{
    /// <summary>
    /// Получить список учёных степеней.
    /// </summary>
    [ProducesResponseType(typeof(PagedResult<AcademicDegreeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [HttpGet]
    public async Task<ActionResult<PagedResult<AcademicDegreeDto>>> ListAsync(
        [FromQuery] string? searchString,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await service.ListAsync(new ListAcademicDegreesQuery(searchString, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Получить учёную степень по идентификатору.
    /// </summary>
    [ProducesResponseType(typeof(AcademicDegreeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AcademicDegreeDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var degree = await service.GetAsync(id, ct);
        return degree is null
            ? Problem(title: "Not Found", detail: "AcademicDegree not found",
                statusCode: StatusCodes.Status404NotFound, instance: id.ToString())
            : Ok(degree);
    }

    /// <summary>
    /// Создать учёную степень.
    /// </summary>
    [ProducesResponseType(typeof(AcademicDegreeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [HttpPost]
    public async Task<ActionResult<AcademicDegreeDto>> CreateAsync(
        [FromBody] UpsertAcademicDegreeItemRequest body,
        CancellationToken ct = default)
    {
        var result = await service.CreateAsync(new UpsertAcademicDegreeCommand(body.Name, body.DisplayName, body.ShortName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                AcademicDegreesError.Validation => Problem(title: "Validation error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                AcademicDegreesError.Conflict => Problem(title: "Conflict", detail: result.Message,
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
    /// Полностью обновить учёную степень (PUT). Name и DisplayName обязательны, ShortName опционален.
    /// </summary>
    [ProducesResponseType(typeof(AcademicDegreeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AcademicDegreeDto>> UpdateAsync(
        Guid id,
        [FromBody] UpsertAcademicDegreeItemRequest body,
        CancellationToken ct = default)
    {
        var result = await service.UpdateAsync(id, new UpsertAcademicDegreeCommand(body.Name, body.DisplayName, body.ShortName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                AcademicDegreesError.NotFound => Problem(title: "Not Found", detail: result.Message,
                    statusCode: StatusCodes.Status404NotFound, instance: id.ToString()),
                AcademicDegreesError.Validation => Problem(title: "Validation error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                AcademicDegreesError.Conflict => Problem(title: "Conflict", detail: result.Message,
                    statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad request", detail: result.Message, statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Частично обновить учёную степень (PATCH). Обновляются только переданные поля.
    /// </summary>
    [ProducesResponseType(typeof(AcademicDegreeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<AcademicDegreeDto>> PatchAsync(
        Guid id,
        [FromBody] PatchAcademicDegreeItemRequest body,
        CancellationToken ct = default)
    {
        var result = await service.PatchAsync(id, new UpsertAcademicDegreeCommand(body.Name, body.DisplayName, body.ShortName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                AcademicDegreesError.NotFound => Problem(title: "Not Found", detail: result.Message,
                    statusCode: StatusCodes.Status404NotFound, instance: id.ToString()),
                AcademicDegreesError.Validation => Problem(title: "Validation error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                AcademicDegreesError.Conflict => Problem(title: "Conflict", detail: result.Message,
                    statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad request", detail: result.Message, statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Удалить учёную степень.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted
            ? NoContent()
            : Problem(title: "Not Found", detail: "AcademicDegree not found",
                statusCode: StatusCodes.Status404NotFound, instance: id.ToString());
    }
}
