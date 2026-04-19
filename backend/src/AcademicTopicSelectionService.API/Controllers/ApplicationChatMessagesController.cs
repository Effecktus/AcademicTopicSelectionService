using Asp.Versioning;
using AcademicTopicSelectionService.API.Extensions;
using AcademicTopicSelectionService.Application.ChatMessages;
using AcademicTopicSelectionService.Application.Dictionaries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// Чат по заявке (студент и научрук из связанного запроса); обновление через polling.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/applications/{applicationId:guid}/messages")]
[Produces("application/json")]
[Authorize]
public sealed class ApplicationChatMessagesController(IChatMessagesService chatService) : ControllerBase
{
    /// <summary>
    /// Список сообщений: без <paramref name="afterId"/> — последние до <paramref name="limit"/> (по умолчанию 50), по времени по возрастанию; с <paramref name="afterId"/> — сообщения новее курсорного.
    /// </summary>
    [ProducesResponseType(typeof(IReadOnlyList<ChatMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet(Name = nameof(GetMessagesAsync))]
    public async Task<ActionResult<IReadOnlyList<ChatMessageDto>>> GetMessagesAsync(
        Guid applicationId,
        [FromQuery] Guid? afterId = null,
        [FromQuery] int? limit = null,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Problem(title: "Unauthorized", detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await chatService.GetMessagesAsync(applicationId, afterId, limit, userId.Value, ct);
        return MapListResult(result);
    }

    /// <summary>
    /// Отправить сообщение в чат заявки.
    /// </summary>
    [ProducesResponseType(typeof(ChatMessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPost]
    public async Task<ActionResult<ChatMessageDto>> SendAsync(
        Guid applicationId,
        [FromBody] SendChatMessageBody body,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Problem(title: "Unauthorized", detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await chatService.SendMessageAsync(
            new SendMessageCommand(applicationId, body.Content ?? string.Empty), userId.Value, ct);

        if (result.Error is not null)
            return MapMutationResult(result);

        return CreatedAtAction(
            nameof(GetMessagesAsync),
            new { applicationId, version = "1.0" },
            result.Value);
    }

    /// <summary>
    /// Отметить все входящие непрочитанные сообщения в чате заявки как прочитанные.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllReadAsync(Guid applicationId, CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Problem(title: "Unauthorized", detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await chatService.MarkAsReadAsync(applicationId, userId.Value, ct);
        if (result.Error is not null)
        {
            return result.Error.Value switch
            {
                ChatMessagesError.NotFound =>
                    Problem(title: "Not Found", detail: result.Message,
                        statusCode: StatusCodes.Status404NotFound),
                ChatMessagesError.Forbidden =>
                    Problem(title: "Forbidden", detail: result.Message,
                        statusCode: StatusCodes.Status403Forbidden),
                _ => Problem(title: "Bad Request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return NoContent();
    }

    private ActionResult<IReadOnlyList<ChatMessageDto>> MapListResult(
        Result<IReadOnlyList<ChatMessageDto>, ChatMessagesError> result)
    {
        if (result.Error is null)
            return Ok(result.Value!);

        return result.Error.Value switch
        {
            ChatMessagesError.NotFound =>
                Problem(title: "Not Found", detail: result.Message,
                    statusCode: StatusCodes.Status404NotFound),
            ChatMessagesError.Forbidden =>
                Problem(title: "Forbidden", detail: result.Message,
                    statusCode: StatusCodes.Status403Forbidden),
            _ => Problem(title: "Bad Request", detail: result.Message,
                statusCode: StatusCodes.Status400BadRequest)
        };
    }

    private ActionResult<ChatMessageDto> MapMutationResult(Result<ChatMessageDto, ChatMessagesError> result)
    {
        return result.Error!.Value switch
        {
            ChatMessagesError.NotFound =>
                Problem(title: "Not Found", detail: result.Message,
                    statusCode: StatusCodes.Status404NotFound),
            ChatMessagesError.Forbidden =>
                Problem(title: "Forbidden", detail: result.Message,
                    statusCode: StatusCodes.Status403Forbidden),
            ChatMessagesError.Validation =>
                Problem(title: "Validation Error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
            _ => Problem(title: "Bad Request", detail: result.Message,
                statusCode: StatusCodes.Status400BadRequest)
        };
    }
}

/// <summary>
/// Тело запроса отправки сообщения в чат.
/// </summary>
public sealed record SendChatMessageBody(string? Content);
