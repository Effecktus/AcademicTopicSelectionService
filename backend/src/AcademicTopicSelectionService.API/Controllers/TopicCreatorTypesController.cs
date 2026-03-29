using Asp.Versioning;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.TopicCreatorTypes;
using Microsoft.AspNetCore.Mvc;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// CRUD для справочника типов создателей тем ВКР (<c>TopicCreatorTypes</c>).
/// </summary>
/// <param name="service">Сервис по работе с базой данных.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/topic-creator-types")]
[Produces("application/json")]
public sealed class TopicCreatorTypesController(ITopicCreatorTypesService service) : ControllerBase
{
    /// <summary>
    /// Получить список типов создателей тем ВКР.
    /// </summary>
    /// <param name="searchString">Поиск по <c>CodeName</c> / <c>DisplayName</c>.</param>
    /// <param name="page">Номер страницы (>= 1).</param>
    /// <param name="pageSize">Размер страницы (1..200).</param>
    /// <param name="ct">Токен отмены.</param>
    [ProducesResponseType(typeof(PagedResult<TopicCreatorTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [HttpGet]
    public async Task<ActionResult<PagedResult<TopicCreatorTypeDto>>> ListAsync(
        [FromQuery] string? searchString,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await service.ListAsync(new ListTopicCreatorTypesQuery(searchString, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Получить тип создателя темы ВКР по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор типа.</param>
    /// <param name="ct">Токен отмены.</param>
    [ProducesResponseType(typeof(TopicCreatorTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TopicCreatorTypeDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var item = await service.GetAsync(id, ct);
        return item is null
            ? Problem(title: "Not Found", detail: "TopicCreatorType not found",
                statusCode: StatusCodes.Status404NotFound, instance: id.ToString())
            : Ok(item);
    }

    /// <summary>
    /// Создать тип создателя темы ВКР.
    /// </summary>
    [ProducesResponseType(typeof(TopicCreatorTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [HttpPost]
    public async Task<ActionResult<TopicCreatorTypeDto>> CreateAsync(
        [FromBody] UpsertNamedItemRequest body,
        CancellationToken ct = default)
    {
        var result = await service.CreateAsync(new UpsertTopicCreatorTypeCommand(body.CodeName, body.DisplayName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                TopicCreatorTypesError.Validation => Problem(title: "Validation error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                TopicCreatorTypesError.Conflict => Problem(title: "Conflict", detail: result.Message,
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
    /// Полностью обновить тип создателя темы ВКР (PUT). Все поля обязательны.
    /// </summary>
    [ProducesResponseType(typeof(TopicCreatorTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TopicCreatorTypeDto>> UpdateAsync(
        Guid id,
        [FromBody] UpsertNamedItemRequest body,
        CancellationToken ct = default)
    {
        var result = await service.UpdateAsync(id, new UpsertTopicCreatorTypeCommand(body.CodeName, body.DisplayName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                TopicCreatorTypesError.NotFound => Problem(title: "Not Found", detail: result.Message,
                    statusCode: StatusCodes.Status404NotFound, instance: id.ToString()),
                TopicCreatorTypesError.Validation => Problem(title: "Validation error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                TopicCreatorTypesError.Conflict => Problem(title: "Conflict", detail: result.Message,
                    statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad request", detail: result.Message, statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Частично обновить тип создателя темы ВКР (PATCH). Обновляются только переданные поля.
    /// </summary>
    [ProducesResponseType(typeof(TopicCreatorTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<TopicCreatorTypeDto>> PatchAsync(
        Guid id,
        [FromBody] PatchNamedItemRequest body,
        CancellationToken ct = default)
    {
        var result = await service.PatchAsync(id, new UpsertTopicCreatorTypeCommand(body.CodeName, body.DisplayName), ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                TopicCreatorTypesError.NotFound => Problem(title: "Not Found", detail: result.Message,
                    statusCode: StatusCodes.Status404NotFound, instance: id.ToString()),
                TopicCreatorTypesError.Validation => Problem(title: "Validation error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                TopicCreatorTypesError.Conflict => Problem(title: "Conflict", detail: result.Message,
                    statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad request", detail: result.Message, statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Удалить тип создателя темы ВКР.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted
            ? NoContent()
            : Problem(title: "Not Found", detail: "TopicCreatorType not found",
                statusCode: StatusCodes.Status404NotFound, instance: id.ToString());
    }
}
