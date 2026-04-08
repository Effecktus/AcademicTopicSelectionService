using Asp.Versioning;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Topics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// Чтение каталога тем ВКР.
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
}
