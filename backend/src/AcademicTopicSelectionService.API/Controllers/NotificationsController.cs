using Asp.Versioning;
using AcademicTopicSelectionService.API.Extensions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// API управления пользовательскими уведомлениями.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/notifications")]
[Produces("application/json")]
[Authorize]
public sealed class NotificationsController(INotificationsService service) : ControllerBase
{
    /// <summary>
    /// Возвращает список уведомлений текущего пользователя.
    /// </summary>
    /// <param name="isRead">Фильтр по статусу прочтения (null — без фильтра).</param>
    /// <param name="page">Номер страницы (с 1).</param>
    /// <param name="pageSize">Размер страницы.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Пагинированный список уведомлений.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<NotificationDto>>> ListAsync(
        [FromQuery] bool? isRead = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Problem(
                title: "Unauthorized",
                detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await service.GetForCurrentUserAsync(
            new ListNotificationsQuery(isRead, page, pageSize),
            userId.Value,
            ct);

        return Ok(result);
    }

    /// <summary>
    /// Отмечает конкретное уведомление как прочитанное.
    /// </summary>
    /// <param name="id">Идентификатор уведомления.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Пустой ответ при успехе.</returns>
    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsReadAsync(Guid id, CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Problem(
                title: "Unauthorized",
                detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await service.MarkAsReadAsync(id, userId.Value, ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                NotificationsError.NotFound => Problem(
                    title: "Not Found",
                    detail: result.Message,
                    statusCode: StatusCodes.Status404NotFound),
                NotificationsError.Forbidden => Problem(
                    title: "Forbidden",
                    detail: result.Message,
                    statusCode: StatusCodes.Status403Forbidden),
                _ => Problem(
                    title: "Bad Request",
                    detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return NoContent();
    }

    /// <summary>
    /// Отмечает все уведомления текущего пользователя как прочитанные.
    /// </summary>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Пустой ответ при успехе.</returns>
    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllAsReadAsync(CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Problem(
                title: "Unauthorized",
                detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        await service.MarkAllAsReadAsync(userId.Value, ct);
        return NoContent();
    }
}
