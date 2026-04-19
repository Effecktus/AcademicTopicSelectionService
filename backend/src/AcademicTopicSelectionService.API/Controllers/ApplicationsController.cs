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
        var userId = User.GetUserId();
        var role = User.GetRoleCode();
        if (userId is null || role is null)
            return Problem(title: "Unauthorized", detail: "User ID or role not found in token",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await service.ListForRoleAsync(
            new ListApplicationsQuery(page, pageSize), role, userId.Value, ct);
        return Ok(result);
    }

    /// <summary>
    /// Заявка по идентификатору (с историей действий).
    /// </summary>
    [ProducesResponseType(typeof(StudentApplicationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StudentApplicationDetailDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var detail = await service.GetDetailAsync(id, ct);
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
        var userId = User.GetUserId();
        if (userId is null)
            return Problem(title: "Unauthorized", detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);

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

        var result = await service.CreateAsync(command, userId.Value, ct);
        if (result.Error is not null)
        {
            return result.Error switch
            {
                ApplicationsError.Validation =>
                    Problem(title: "Validation Error", detail: result.Message,
                        statusCode: StatusCodes.Status400BadRequest),
                ApplicationsError.NotFound =>
                    Problem(title: "Not Found", detail: result.Message,
                        statusCode: StatusCodes.Status404NotFound),
                ApplicationsError.Conflict =>
                    Problem(title: "Conflict", detail: result.Message,
                        statusCode: StatusCodes.Status409Conflict),
                ApplicationsError.Forbidden =>
                    Problem(title: "Forbidden", detail: result.Message,
                        statusCode: StatusCodes.Status403Forbidden),
                _ => Problem(title: "Bad Request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return CreatedAtAction(nameof(GetAsync), new { id = result.Value!.Id, version = "1.0" }, result.Value);
    }

    /// <summary>
    /// Преподаватель одобряет заявку: Pending → ApprovedBySupervisor.
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
        var userId = User.GetUserId();
        if (userId is null)
            return Problem(title: "Unauthorized", detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);

        command ??= new ApproveBySupervisorCommand(null);

        var result = await service.ApproveBySupervisorAsync(id, command, userId.Value, ct);
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
        var userId = User.GetUserId();
        if (userId is null)
            return Problem(title: "Unauthorized", detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await service.RejectBySupervisorAsync(id, command, userId.Value, ct);
        return MapResult(result);
    }

    /// <summary>
    /// Преподаватель передаёт заявку заведующему кафедрой: ApprovedBySupervisor → PendingDepartmentHead.
    /// </summary>
    [ProducesResponseType(typeof(StudentApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPut("{id:guid}/submit-to-department-head")]
    public async Task<ActionResult<StudentApplicationDto>> SubmitToDepartmentHeadAsync(
        Guid id,
        [FromBody] SubmitToDepartmentHeadCommand? command = null,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Problem(title: "Unauthorized", detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);

        command ??= new SubmitToDepartmentHeadCommand(null);

        var result = await service.SubmitToDepartmentHeadAsync(id, command, userId.Value, ct);
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
        var userId = User.GetUserId();
        if (userId is null)
            return Problem(title: "Unauthorized", detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);

        command ??= new ApproveByDepartmentHeadCommand(null);

        var result = await service.ApproveByDepartmentHeadAsync(id, command, userId.Value, ct);
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
        var userId = User.GetUserId();
        if (userId is null)
            return Problem(title: "Unauthorized", detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await service.RejectByDepartmentHeadAsync(id, command, userId.Value, ct);
        return MapResult(result);
    }

    /// <summary>
    /// Студент отменяет заявку: Pending или ApprovedBySupervisor → Cancelled.
    /// Нельзя отменить после передачи заведующему.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpPut("{id:guid}/cancel")]
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
                ApplicationsError.Validation =>
                    Problem(title: "Validation Error", detail: result.Message,
                        statusCode: StatusCodes.Status400BadRequest),
                ApplicationsError.NotFound =>
                    Problem(title: "Not Found", detail: result.Message,
                        statusCode: StatusCodes.Status404NotFound),
                ApplicationsError.Forbidden =>
                    Problem(title: "Forbidden", detail: result.Message,
                        statusCode: StatusCodes.Status403Forbidden),
                ApplicationsError.InvalidTransition =>
                    Problem(title: "Conflict", detail: result.Message,
                        statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad Request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return NoContent();
    }

    // ======================== Helpers ========================

    private ActionResult<StudentApplicationDto> MapResult(
        Result<StudentApplicationDto, ApplicationsError> result)
    {
        if (result.Error is not null)
        {
            return result.Error switch
            {
                ApplicationsError.Validation =>
                    Problem(title: "Validation Error", detail: result.Message,
                        statusCode: StatusCodes.Status400BadRequest),
                ApplicationsError.NotFound =>
                    Problem(title: "Not Found", detail: result.Message,
                        statusCode: StatusCodes.Status404NotFound),
                ApplicationsError.Forbidden =>
                    Problem(title: "Forbidden", detail: result.Message,
                        statusCode: StatusCodes.Status403Forbidden),
                ApplicationsError.Conflict =>
                    Problem(title: "Conflict", detail: result.Message,
                        statusCode: StatusCodes.Status409Conflict),
                ApplicationsError.InvalidTransition =>
                    Problem(title: "Invalid Transition", detail: result.Message,
                        statusCode: StatusCodes.Status400BadRequest),
                ApplicationsError.SupervisorLimitExceeded =>
                    Problem(title: "Conflict", detail: result.Message,
                        statusCode: StatusCodes.Status409Conflict),
                _ => Problem(title: "Bad Request", detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest)
            };
        }

        return Ok(result.Value!);
    }
}
