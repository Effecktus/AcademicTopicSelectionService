using Asp.Versioning;
using AcademicTopicSelectionService.Application.ApplicationActions;
using AcademicTopicSelectionService.Application.Dictionaries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// API для работы с действиями по заявкам (<c>ApplicationActions</c>).
/// Действия описывают историю согласований заявки студента.
/// </summary>
/// <param name="service">Сервис действий по заявкам.</param>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/application-actions")]
[Produces("application/json")]
[Authorize]
public sealed class ApplicationActionsController(IApplicationActionsService service) : ControllerBase
{
    /// <summary>
    /// Получить список действий по заявке.
    /// </summary>
    /// <param name="applicationId">Идентификатор заявки (обязателен).</param>
    /// <param name="page">Номер страницы (>= 1).</param>
    /// <param name="pageSize">Размер страницы (1..200).</param>
    /// <param name="ct">Токен отмены.</param>
    [ProducesResponseType(typeof(PagedResult<ApplicationActionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [HttpGet]
    public async Task<ActionResult<PagedResult<ApplicationActionDto>>> ListAsync(
        [FromQuery] Guid applicationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (applicationId == Guid.Empty)
        {
            return Problem(title: "Validation error", detail: "applicationId is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await service.ListByApplicationAsync(
            new ListApplicationActionsQuery(applicationId, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Получить действие по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор действия.</param>
    /// <param name="ct">Токен отмены.</param>
    [ProducesResponseType(typeof(ApplicationActionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApplicationActionDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var action = await service.GetAsync(id, ct);
        return action is null
            ? Problem(title: "Not Found", detail: "ApplicationAction not found",
                statusCode: StatusCodes.Status404NotFound, instance: id.ToString())
            : Ok(action);
    }

    /// <summary>
    /// Создать действие по заявке. Статус устанавливается в <c>Pending</c> автоматически.
    /// </summary>
    [ProducesResponseType(typeof(ApplicationActionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPost]
    public async Task<ActionResult<ApplicationActionDto>> CreateAsync(
        [FromBody] CreateApplicationActionRequest body,
        CancellationToken ct = default)
    {
        var result = await service.CreateAsync(
            new CreateApplicationActionCommand(body.ApplicationId, body.ResponsibleId, body.Comment), ct);

        if (result.Error is not null)
        {
            return result.Error switch
            {
                ApplicationActionsError.Validation => Problem(title: "Validation error",
                    detail: result.Message, statusCode: StatusCodes.Status400BadRequest),
                ApplicationActionsError.ApplicationNotFound => Problem(title: "Not Found",
                    detail: result.Message, statusCode: StatusCodes.Status404NotFound),
                ApplicationActionsError.ResponsibleUserNotFound => Problem(title: "Not Found",
                    detail: result.Message, statusCode: StatusCodes.Status404NotFound),
                ApplicationActionsError.StatusNotFound => Problem(title: "Not Found",
                    detail: result.Message, statusCode: StatusCodes.Status404NotFound),
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
    /// Обновить статус и/или комментарий действия (PATCH).
    /// Передавайте только поля, которые нужно изменить.
    /// </summary>
    [ProducesResponseType(typeof(ApplicationActionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ApplicationActionDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateApplicationActionRequest body,
        CancellationToken ct = default)
    {
        var result = await service.UpdateAsync(id,
            new UpdateApplicationActionCommand(body.StatusId, body.Comment), ct);

        if (result.Error is not null)
        {
            return result.Error switch
            {
                ApplicationActionsError.NotFound => Problem(title: "Not Found",
                    detail: result.Message, statusCode: StatusCodes.Status404NotFound, instance: id.ToString()),
                ApplicationActionsError.StatusNotFound => Problem(title: "Not Found",
                    detail: result.Message, statusCode: StatusCodes.Status404NotFound),
                ApplicationActionsError.Validation => Problem(title: "Validation error",
                    detail: result.Message, statusCode: StatusCodes.Status400BadRequest),
                _ => Problem(title: "Bad request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Удалить действие по заявке.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted
            ? NoContent()
            : Problem(title: "Not Found", detail: "ApplicationAction not found",
                statusCode: StatusCodes.Status404NotFound, instance: id.ToString());
    }
}
