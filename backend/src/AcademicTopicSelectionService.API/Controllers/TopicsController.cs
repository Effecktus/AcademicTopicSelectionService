using Asp.Versioning;
using AcademicTopicSelectionService.API.Extensions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Topics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// Управление темами ВКР (создание, чтение, обновление, удаление).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/topics")]
[Produces("application/json")]
[Authorize]
public sealed class TopicsController(ITopicsService service) : ControllerBase
{
    /// <summary>
    /// Список тем с фильтрами и сортировкой.
    /// </summary>
    [ProducesResponseType(typeof(PagedResult<TopicDto>), StatusCodes.Status200OK)]
    [HttpGet]
    public async Task<ActionResult<PagedResult<TopicDto>>> ListAsync(
        [FromQuery] string? query,
        [FromQuery] string? statusCodeName,
        [FromQuery] Guid? createdByUserId,
        [FromQuery] string? creatorTypeCodeName,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await service.ListAsync(
            new ListTopicsQuery(query, statusCodeName, createdByUserId, creatorTypeCodeName, sort, page, pageSize),
            ct);
        return Ok(result);
    }

    /// <summary>
    /// Тема по идентификатору.
    /// </summary>
    [ProducesResponseType(typeof(TopicDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TopicDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var topic = await service.GetAsync(id, ct);
        return topic is null
            ? Problem(title: "Not Found", detail: "Topic not found", statusCode: StatusCodes.Status404NotFound,
                instance: id.ToString())
            : Ok(topic);
    }

    /// <summary>
    /// Создать тему ВКР.
    /// </summary>
    /// <remarks>
    /// Только для аутентифицированных пользователей.
    /// <c>creatorTypeCodeName</c>: <c>Teacher</c> или <c>Student</c>.
    /// <c>statusCodeName</c>: <c>Active</c> (по умолчанию) или <c>Inactive</c>.
    /// </remarks>
    [ProducesResponseType(typeof(TopicDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [HttpPost]
    public async Task<ActionResult<TopicDto>> CreateAsync(
        [FromBody] CreateTopicCommand command,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Problem(title: "Unauthorized", detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await service.CreateAsync(command, userId.Value, ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                TopicsError.Validation =>
                    Problem(title: "Validation Error", detail: result.Message,
                        statusCode: StatusCodes.Status400BadRequest),
                TopicsError.NotFound =>
                    Problem(title: "Not Found", detail: result.Message,
                        statusCode: StatusCodes.Status404NotFound),
                TopicsError.Forbidden =>
                    Problem(title: "Forbidden", detail: result.Message,
                        statusCode: StatusCodes.Status403Forbidden),
                _ => Problem(title: "Bad Request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return CreatedAtAction(nameof(GetAsync), new { id = result.Value!.Id, version = "1.0" }, result.Value);
    }

    /// <summary>
    /// Полностью заменить тему ВКР.
    /// </summary>
    /// <remarks>
    /// Только автор темы может заменить.
    /// <c>statusCodeName</c> обязателен.
    /// </remarks>
    [ProducesResponseType(typeof(TopicDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TopicDto>> ReplaceAsync(
        Guid id,
        [FromBody] ReplaceTopicCommand command,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Problem(title: "Unauthorized", detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await service.ReplaceAsync(id, command, userId.Value, ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                TopicsError.Validation =>
                    Problem(title: "Validation Error", detail: result.Message,
                        statusCode: StatusCodes.Status400BadRequest),
                TopicsError.NotFound =>
                    Problem(title: "Not Found", detail: result.Message,
                        statusCode: StatusCodes.Status404NotFound),
                TopicsError.Forbidden =>
                    Problem(title: "Forbidden", detail: result.Message,
                        statusCode: StatusCodes.Status403Forbidden),
                _ => Problem(title: "Bad Request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Частично обновить тему ВКР.
    /// </summary>
    /// <remarks>
    /// Только автор темы может редактировать.
    /// Указываются только те поля, которые нужно изменить.
    /// </remarks>
    [ProducesResponseType(typeof(TopicDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<TopicDto>> PatchAsync(
        Guid id,
        [FromBody] UpdateTopicCommand command,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Problem(title: "Unauthorized", detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await service.UpdateAsync(id, command, userId.Value, ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                TopicsError.Validation =>
                    Problem(title: "Validation Error", detail: result.Message,
                        statusCode: StatusCodes.Status400BadRequest),
                TopicsError.NotFound =>
                    Problem(title: "Not Found", detail: result.Message,
                        statusCode: StatusCodes.Status404NotFound),
                TopicsError.Forbidden =>
                    Problem(title: "Forbidden", detail: result.Message,
                        statusCode: StatusCodes.Status403Forbidden),
                _ => Problem(title: "Bad Request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Удалить тему ВКР.
    /// </summary>
    /// <remarks>
    /// Только автор темы может удалить. Нельзя удалить тему с заявками.
    /// </remarks>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Problem(title: "Unauthorized", detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await service.DeleteAsync(id, userId.Value, ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                TopicsError.Validation =>
                    Problem(title: "Validation Error", detail: result.Message,
                        statusCode: StatusCodes.Status400BadRequest),
                TopicsError.NotFound =>
                    Problem(title: "Not Found", detail: result.Message,
                        statusCode: StatusCodes.Status404NotFound),
                TopicsError.Forbidden =>
                    Problem(title: "Forbidden", detail: result.Message,
                        statusCode: StatusCodes.Status403Forbidden),
                _ => Problem(title: "Bad Request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return NoContent();
    }
}
