using Asp.Versioning;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.StudyGroups;
using Microsoft.AspNetCore.Mvc;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// CRUD для справочника учебных групп (<c>StudyGroups</c>).
/// </summary>
/// <param name="service">Сервис по работе с базой данных.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/study-groups")]
[Produces("application/json")]
public sealed class StudyGroupsController(IStudyGroupsService service) : ControllerBase
{
    /// <summary>
    /// Получить список учебных групп.
    /// </summary>
    /// <param name="codeName">Фильтр по точному номеру группы (опционально).</param>
    /// <param name="page">Номер страницы (>= 1).</param>
    /// <param name="pageSize">Размер страницы (1..200).</param>
    /// <param name="ct">Токен отмены.</param>
    [ProducesResponseType(typeof(PagedResult<StudyGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [HttpGet]
    public async Task<ActionResult<PagedResult<StudyGroupDto>>> ListAsync(
        [FromQuery] int? codeName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await service.ListAsync(new ListStudyGroupsQuery(codeName, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Получить группу по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор группы.</param>
    /// <param name="ct">Токен отмены.</param>
    [ProducesResponseType(typeof(StudyGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StudyGroupDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var group = await service.GetAsync(id, ct);
        return group is null
            ? Problem(title: "Not Found", detail: "StudyGroup not found", statusCode: StatusCodes.Status404NotFound,
                instance: id.ToString())
            : Ok(group);
    }

    /// <summary>
    /// Создать учебную группу.
    /// </summary>
    [ProducesResponseType(typeof(StudyGroupDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [HttpPost]
    public async Task<ActionResult<StudyGroupDto>> CreateAsync(
        [FromBody] UpsertStudyGroupRequest body,
        CancellationToken ct = default)
    {
        var result = await service.CreateAsync(new UpsertStudyGroupCommand(body.CodeName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                StudyGroupsError.Validation => Problem(title: "Validation error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                StudyGroupsError.Conflict => Problem(title: "Conflict", detail: result.Message,
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
    /// Полностью обновить группу (PUT). Поле CodeName обязательно.
    /// </summary>
    [ProducesResponseType(typeof(StudyGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StudyGroupDto>> UpdateAsync(
        Guid id,
        [FromBody] UpsertStudyGroupRequest body,
        CancellationToken ct = default)
    {
        var result = await service.UpdateAsync(id, new UpsertStudyGroupCommand(body.CodeName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                StudyGroupsError.NotFound => Problem(title: "Not Found", detail: result.Message,
                    statusCode: StatusCodes.Status404NotFound, instance: id.ToString()),
                StudyGroupsError.Validation => Problem(title: "Validation error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                StudyGroupsError.Conflict => Problem(title: "Conflict", detail: result.Message,
                    statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad request", detail: result.Message, statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Частично обновить группу (PATCH). Обновляются только переданные поля.
    /// </summary>
    [ProducesResponseType(typeof(StudyGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<StudyGroupDto>> PatchAsync(
        Guid id,
        [FromBody] PatchStudyGroupRequest body,
        CancellationToken ct = default)
    {
        var result = await service.PatchAsync(id, new UpsertStudyGroupCommand(body.CodeName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                StudyGroupsError.NotFound => Problem(title: "Not Found", detail: result.Message,
                    statusCode: StatusCodes.Status404NotFound, instance: id.ToString()),
                StudyGroupsError.Validation => Problem(title: "Validation error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                StudyGroupsError.Conflict => Problem(title: "Conflict", detail: result.Message,
                    statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad request", detail: result.Message, statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Удалить группу.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted
            ? NoContent()
            : Problem(title: "Not Found", detail: "StudyGroup not found", statusCode: StatusCodes.Status404NotFound,
                instance: id.ToString());
    }
}
