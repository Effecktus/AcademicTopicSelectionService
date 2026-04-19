using Asp.Versioning;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.API.Extensions;
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        if (!TryResolveActor(out var actor))
            return Unauthorized();

        var result = await service.ListByApplicationAsync(
            new ListApplicationActionsQuery(applicationId, page, pageSize), actor, ct);

        if (result.Error is not null)
        {
            return result.Error switch
            {
                ApplicationActionsError.ApplicationNotFound => Problem(title: "Not Found",
                    detail: result.Message, statusCode: StatusCodes.Status404NotFound),
                ApplicationActionsError.Forbidden => Forbid(),
                _ => Problem(title: "Bad request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Получить действие по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор действия.</param>
    /// <param name="ct">Токен отмены.</param>
    [ProducesResponseType(typeof(ApplicationActionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApplicationActionDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        if (!TryResolveActor(out var actor))
            return Unauthorized();

        var result = await service.GetAsync(id, actor, ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                ApplicationActionsError.NotFound => Problem(title: "Not Found", detail: result.Message,
                    statusCode: StatusCodes.Status404NotFound, instance: id.ToString()),
                ApplicationActionsError.Forbidden => Forbid(),
                _ => Problem(title: "Bad request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Создать действие по заявке. Статус устанавливается в <c>Pending</c> автоматически.
    /// </summary>
    [ProducesResponseType(typeof(ApplicationActionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [HttpPost]
    public async Task<ActionResult<ApplicationActionDto>> CreateAsync(
        [FromBody] CreateApplicationActionRequest body,
        CancellationToken ct = default)
    {
        if (!TryResolveActor(out var actor))
            return Unauthorized();

        var result = await service.CreateAsync(
            new CreateApplicationActionCommand(body.ApplicationId, body.ResponsibleId, body.Comment), actor, ct);

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
                ApplicationActionsError.Forbidden => Forbid(),
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ApplicationActionDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateApplicationActionRequest body,
        CancellationToken ct = default)
    {
        if (!TryResolveActor(out var actor))
            return Unauthorized();

        var result = await service.UpdateAsync(id,
            new UpdateApplicationActionCommand(body.StatusId, body.Comment), actor, ct);

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
                ApplicationActionsError.Forbidden => Forbid(),
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!TryResolveActor(out var actor))
            return Unauthorized();

        var result = await service.DeleteAsync(id, actor, ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                ApplicationActionsError.NotFound => Problem(title: "Not Found", detail: result.Message,
                    statusCode: StatusCodes.Status404NotFound, instance: id.ToString()),
                ApplicationActionsError.Forbidden => Forbid(),
                _ => Problem(title: "Bad request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return NoContent();
    }

    private bool TryResolveActor(out ApplicationActionsActor actor)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            actor = default;
            return false;
        }

        actor = new ApplicationActionsActor(userId.Value, User.IsInRole(AppRoles.Admin));
        return true;
    }
}
