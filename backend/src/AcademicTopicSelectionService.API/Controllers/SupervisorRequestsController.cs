using Asp.Versioning;
using AcademicTopicSelectionService.API.Extensions;
using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.SupervisorRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// API управления запросами на выбор научного руководителя.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/supervisor-requests")]
[Produces("application/json")]
[Authorize]
public sealed class SupervisorRequestsController(ISupervisorRequestsService service) : ControllerBase
{
    /// <summary>
    /// Возвращает список запросов, отфильтрованный по роли пользователя.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SupervisorRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SupervisorRequestDto>>> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sort = null,
        [FromQuery] DateTimeOffset? createdFromUtc = null,
        [FromQuery] DateTimeOffset? createdToUtc = null,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        var role = User.GetRoleCode();
        if (userId is null || role is null)
            return Problem(title: "Unauthorized", detail: "User ID or role not found in token",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await service.ListForRoleAsync(
            new ListSupervisorRequestsQuery(
                Page: page,
                PageSize: pageSize,
                Sort: sort,
                CreatedFromUtc: createdFromUtc,
                CreatedToUtc: createdToUtc),
            role,
            userId.Value,
            ct);
        return Ok(result);
    }

    /// <summary>
    /// Возвращает детальную информацию по запросу.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SupervisorRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupervisorRequestDetailDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var detail = await service.GetDetailAsync(id, ct);
        return detail is null
            ? Problem(title: "Not Found", detail: "Supervisor request not found",
                statusCode: StatusCodes.Status404NotFound, instance: id.ToString())
            : Ok(detail);
    }

    /// <summary>
    /// Создаёт новый запрос студента на назначение научного руководителя.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SupervisorRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SupervisorRequestDto>> CreateAsync(
        [FromBody] CreateSupervisorRequestCommand command,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Problem(title: "Unauthorized", detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await service.CreateAsync(command, userId.Value, ct);
        if (result.Error is not null)
            return MapResult(result);

        return CreatedAtAction(nameof(GetAsync), new { id = result.Value!.Id, version = "1.0" }, result.Value);
    }

    /// <summary>
    /// Подтверждает запрос преподавателем.
    /// </summary>
    [HttpPut("{id:guid}/approve")]
    [ProducesResponseType(typeof(SupervisorRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupervisorRequestDto>> ApproveAsync(Guid id, CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Problem(title: "Unauthorized", detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await service.ApproveAsync(id, userId.Value, ct);
        return MapResult(result);
    }

    /// <summary>
    /// Отклоняет запрос преподавателем с обязательным комментарием.
    /// </summary>
    [HttpPut("{id:guid}/reject")]
    [ProducesResponseType(typeof(SupervisorRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupervisorRequestDto>> RejectAsync(
        Guid id,
        [FromBody] RejectSupervisorRequestCommand command,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Problem(title: "Unauthorized", detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await service.RejectAsync(id, command, userId.Value, ct);
        return MapResult(result);
    }

    /// <summary>
    /// Отменяет запрос студентом-владельцем.
    /// </summary>
    [HttpPut("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Problem(title: "Unauthorized", detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await service.CancelAsync(id, userId.Value, ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                SupervisorRequestsError.Validation =>
                    Problem(title: "Validation Error", detail: result.Message,
                        statusCode: StatusCodes.Status400BadRequest),
                SupervisorRequestsError.NotFound =>
                    Problem(title: "Not Found", detail: result.Message,
                        statusCode: StatusCodes.Status404NotFound),
                SupervisorRequestsError.Forbidden =>
                    Problem(title: "Forbidden", detail: result.Message,
                        statusCode: StatusCodes.Status403Forbidden),
                SupervisorRequestsError.InvalidTransition =>
                    Problem(title: "Conflict", detail: result.Message,
                        statusCode: StatusCodes.Status409Conflict),
                SupervisorRequestsError.Conflict =>
                    Problem(title: "Conflict", detail: result.Message,
                        statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad Request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return NoContent();
    }

    /// <summary>
    /// Преобразует бизнес-результат в HTTP-ответ.
    /// </summary>
    private ActionResult<SupervisorRequestDto> MapResult(Result<SupervisorRequestDto, SupervisorRequestsError> result)
    {
        if (result.Error is null)
            return Ok(result.Value!);

        return result.Error switch
        {
            SupervisorRequestsError.Validation =>
                Problem(title: "Validation Error", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
            SupervisorRequestsError.NotFound =>
                Problem(title: "Not Found", detail: result.Message,
                    statusCode: StatusCodes.Status404NotFound),
            SupervisorRequestsError.Forbidden =>
                Problem(title: "Forbidden", detail: result.Message,
                    statusCode: StatusCodes.Status403Forbidden),
            SupervisorRequestsError.Conflict =>
                Problem(title: "Conflict", detail: result.Message,
                    statusCode: StatusCodes.Status409Conflict),
            SupervisorRequestsError.InvalidTransition =>
                Problem(title: "Invalid Transition", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
            _ => Problem(title: "Bad Request", detail: result.Message,
                statusCode: StatusCodes.Status400BadRequest)
        };
    }
}
