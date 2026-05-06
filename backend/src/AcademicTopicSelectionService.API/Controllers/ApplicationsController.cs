using Asp.Versioning;
using AcademicTopicSelectionService.API.Extensions;
using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.StudentApplications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// Управление заявками студентов на темы ВКР.
/// Жизненный цикл: создание → одобрение преподавателем → утверждение зав. кафедрой.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/applications")]
[Produces("application/json")]
[Authorize]
public sealed class ApplicationsController(IStudentApplicationsService service) : ControllerBase
{
    /// <summary>
    /// Список заявок (отфильтрован по роли пользователя).
    /// Студент — свои заявки. Преподаватель — заявки по своим темам.
    /// Зав. кафедрой — заявки кафедры. Админ — все.
    /// </summary>
    [ProducesResponseType(typeof(PagedResult<StudentApplicationDto>), StatusCodes.Status200OK)]
    [HttpGet]
    public async Task<ActionResult<PagedResult<StudentApplicationDto>>> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (UnauthorizedActor(out var userId, out var role) is { } unauthorized)
            return unauthorized;

        var result = await service.ListForRoleAsync(
            new ListApplicationsQuery(page, pageSize), role, userId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Заявка по идентификатору (с историей действий).
    /// </summary>
    [ProducesResponseType(typeof(StudentApplicationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StudentApplicationDetailDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        if (UnauthorizedActor(out var userId, out var role) is { } unauthorized)
            return unauthorized;

        var detail = await service.GetDetailForViewerAsync(id, role, userId, ct);
        return detail is null
            ? Problem(title: "Not Found", detail: "Application not found",
                statusCode: StatusCodes.Status404NotFound, instance: id.ToString())
            : Ok(detail);
    }

    /// <summary>
    /// Подать заявку на тему ВКР. Только для студентов.
    /// </summary>
    [ProducesResponseType(typeof(StudentApplicationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [HttpPost]
    public async Task<ActionResult<StudentApplicationDto>> CreateAsync(
        [FromBody] CreateApplicationCommand command,
        CancellationToken ct = default)
    {
        if (UnauthorizedUserId(out var userId) is { } unauthorized)
            return unauthorized;

        var hasTopicId = command.TopicId.HasValue && command.TopicId.Value != Guid.Empty;
        var hasProposedTitle = !string.IsNullOrWhiteSpace(command.ProposedTitle);
        if (hasTopicId == hasProposedTitle)
            return Problem(
                title: "Validation Error",
                detail: "Pass either TopicId or ProposedTitle, but not both",
                statusCode: StatusCodes.Status400BadRequest);

        if (command.SupervisorRequestId == Guid.Empty)
            return Problem(title: "Validation Error", detail: "SupervisorRequestId is required",
                statusCode: StatusCodes.Status400BadRequest);

        var result = await service.CreateAsync(command, userId, ct);
        if (result.Error is not null)
            return ProblemForApplicationsError(result.Error.Value, result.Message);

        return CreatedAtAction(nameof(GetAsync), new { id = result.Value!.Id, version = "1.0" }, result.Value);
    }

    /// <summary>
    /// Студент передаёт заявку научному руководителю: OnEditing → Pending.
    /// </summary>
    [ProducesResponseType(typeof(StudentApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPut("{id:guid}/submit-to-supervisor")]
    public async Task<ActionResult<StudentApplicationDto>> SubmitToSupervisorAsync(Guid id, CancellationToken ct = default)
    {
        if (UnauthorizedUserId(out var userId) is { } unauthorized)
            return unauthorized;

        var result = await service.SubmitToSupervisorAsync(id, userId, ct);
        return MapResult(result);
    }

    /// <summary>
    /// Студент обновляет название и описание темы по заявке (только в статусе OnEditing).
    /// </summary>
    [ProducesResponseType(typeof(StudentApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPatch("{id:guid}/topic")]
    public async Task<ActionResult<StudentApplicationDto>> UpdateTopicAsync(
        Guid id,
        [FromBody] UpdateApplicationTopicCommand command,
        CancellationToken ct = default)
    {
        if (UnauthorizedUserId(out var userId) is { } unauthorized)
            return unauthorized;

        var result = await service.UpdateTopicAsync(id, command, userId, ct);
        return MapResult(result);
    }

    /// <summary>
    /// Преподаватель одобряет заявку и передаёт её заведующему: Pending → PendingDepartmentHead.
    /// </summary>
    [ProducesResponseType(typeof(StudentApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPut("{id:guid}/approve")]
    public async Task<ActionResult<StudentApplicationDto>> ApproveBySupervisorAsync(
        Guid id,
        [FromBody] ApproveBySupervisorCommand? command = null,
        CancellationToken ct = default)
    {
        if (UnauthorizedUserId(out var userId) is { } unauthorized)
            return unauthorized;

        command ??= new ApproveBySupervisorCommand(null);

        var result = await service.ApproveBySupervisorAsync(id, command, userId, ct);
        return MapResult(result);
    }

    /// <summary>
    /// Преподаватель отклоняет заявку: Pending → RejectedBySupervisor.
    /// </summary>
    [ProducesResponseType(typeof(StudentApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPut("{id:guid}/reject")]
    public async Task<ActionResult<StudentApplicationDto>> RejectBySupervisorAsync(
        Guid id,
        [FromBody] RejectBySupervisorCommand command,
        CancellationToken ct = default)
    {
        if (UnauthorizedUserId(out var userId) is { } unauthorized)
            return unauthorized;

        var result = await service.RejectBySupervisorAsync(id, command, userId, ct);
        return MapResult(result);
    }

    /// <summary>
    /// Преподаватель возвращает заявку на редактирование: Pending → OnEditing.
    /// </summary>
    [ProducesResponseType(typeof(StudentApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPut("{id:guid}/return-for-editing")]
    public async Task<ActionResult<StudentApplicationDto>> ReturnForEditingBySupervisorAsync(
        Guid id,
        [FromBody] ReturnApplicationForEditingCommand command,
        CancellationToken ct = default)
    {
        if (UnauthorizedUserId(out var userId) is { } unauthorized)
            return unauthorized;

        var result = await service.ReturnForEditingBySupervisorAsync(id, command, userId, ct);
        return MapResult(result);
    }

    /// <summary>
    /// Заведующий кафедрой утверждает заявку: PendingDepartmentHead → ApprovedByDepartmentHead.
    /// </summary>
    [ProducesResponseType(typeof(StudentApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPut("{id:guid}/department-head-approve")]
    public async Task<ActionResult<StudentApplicationDto>> ApproveByDepartmentHeadAsync(
        Guid id,
        [FromBody] ApproveByDepartmentHeadCommand? command = null,
        CancellationToken ct = default)
    {
        if (UnauthorizedUserId(out var userId) is { } unauthorized)
            return unauthorized;

        command ??= new ApproveByDepartmentHeadCommand(null);

        var result = await service.ApproveByDepartmentHeadAsync(id, command, userId, ct);
        return MapResult(result);
    }

    /// <summary>
    /// Заведующий кафедрой отклоняет заявку: PendingDepartmentHead → RejectedByDepartmentHead.
    /// </summary>
    [ProducesResponseType(typeof(StudentApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPut("{id:guid}/department-head-reject")]
    public async Task<ActionResult<StudentApplicationDto>> RejectByDepartmentHeadAsync(
        Guid id,
        [FromBody] RejectByDepartmentHeadCommand command,
        CancellationToken ct = default)
    {
        if (UnauthorizedUserId(out var userId) is { } unauthorized)
            return unauthorized;

        var result = await service.RejectByDepartmentHeadAsync(id, command, userId, ct);
        return MapResult(result);
    }

    /// <summary>
    /// Заведующий кафедрой возвращает заявку на редактирование: PendingDepartmentHead → OnEditing.
    /// </summary>
    [ProducesResponseType(typeof(StudentApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPut("{id:guid}/department-head-return-for-editing")]
    public async Task<ActionResult<StudentApplicationDto>> ReturnForEditingByDepartmentHeadAsync(
        Guid id,
        [FromBody] ReturnApplicationForEditingCommand command,
        CancellationToken ct = default)
    {
        if (UnauthorizedUserId(out var userId) is { } unauthorized)
            return unauthorized;

        var result = await service.ReturnForEditingByDepartmentHeadAsync(id, command, userId, ct);
        return MapResult(result);
    }

    /// <summary>
    /// Студент отменяет заявку: Pending, ApprovedBySupervisor или OnEditing → Cancelled.
    /// Нельзя отменить после передачи заведующему.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> CancelAsync(Guid id, CancellationToken ct = default)
    {
        if (UnauthorizedUserId(out var userId) is { } unauthorized)
            return unauthorized;

        var result = await service.CancelAsync(id, userId, ct);
        if (result.Error is not null)
            return ProblemForApplicationsError(result.Error.Value, result.Message);

        return NoContent();
    }

    // ======================== Helpers ========================

    private ActionResult? UnauthorizedUserId(out Guid userId)
    {
        var id = User.GetUserId();
        if (id is null)
        {
            userId = default;
            return Problem(title: "Unauthorized", detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        userId = id.Value;
        return null;
    }

    private ActionResult? UnauthorizedActor(out Guid userId, out string role)
    {
        var id = User.GetUserId();
        var r = User.GetRoleCode();
        if (id is null || r is null)
        {
            userId = default;
            role = string.Empty;
            return Problem(title: "Unauthorized", detail: "User ID or role not found in token",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        userId = id.Value;
        role = r;
        return null;
    }

    private ActionResult ProblemForApplicationsError(ApplicationsError error, string? detail) =>
        error switch
        {
            ApplicationsError.Validation => Problem(title: "Validation Error", detail: detail,
                statusCode: StatusCodes.Status400BadRequest),
            ApplicationsError.NotFound => Problem(title: "Not Found", detail: detail,
                statusCode: StatusCodes.Status404NotFound),
            ApplicationsError.Forbidden => Problem(title: "Forbidden", detail: detail,
                statusCode: StatusCodes.Status403Forbidden),
            ApplicationsError.Conflict => Problem(title: "Conflict", detail: detail,
                statusCode: StatusCodes.Status409Conflict),
            ApplicationsError.InvalidTransition => Problem(title: "Conflict", detail: detail,
                statusCode: StatusCodes.Status409Conflict),
            ApplicationsError.SupervisorLimitExceeded => Problem(title: "Conflict", detail: detail,
                statusCode: StatusCodes.Status409Conflict),
            _ => Problem(title: "Bad Request", detail: detail,
                statusCode: StatusCodes.Status400BadRequest)
        };

    private ActionResult<StudentApplicationDto> MapResult(
        Result<StudentApplicationDto, ApplicationsError> result) =>
        result.Error is not null
            ? ProblemForApplicationsError(result.Error.Value, result.Message)
            : Ok(result.Value!);
}
