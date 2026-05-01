using Asp.Versioning;
using AcademicTopicSelectionService.API.Extensions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Teachers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// Чтение каталога преподавателей.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/teachers")]
[Produces("application/json")]
[Authorize]
public sealed class TeachersController(ITeachersService service) : ControllerBase
{
    /// <summary>
    /// Список преподавателей (только активные пользователи).
    /// </summary>
    [ProducesResponseType(typeof(PagedResult<TeacherDto>), StatusCodes.Status200OK)]
    [HttpGet]
    public async Task<ActionResult<PagedResult<TeacherDto>>> ListAsync(
        [FromQuery] string? query,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        var role = User.GetRoleCode();
        if (userId is null || role is null)
            return Problem(
                title: "Unauthorized",
                detail: "User ID or role not found in token",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await service.ListAsync(
            new ListTeachersQuery(query, page, pageSize, sort),
            role,
            userId.Value,
            ct);
        return Ok(result);
    }

    /// <summary>
    /// Преподаватель по идентификатору записи <c>Teachers.Id</c>.
    /// </summary>
    [ProducesResponseType(typeof(TeacherDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TeacherDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var teacher = await service.GetAsync(id, ct);
        return teacher is null
            ? Problem(title: "Not Found", detail: "Teacher not found", statusCode: StatusCodes.Status404NotFound,
                instance: id.ToString())
            : Ok(teacher);
    }
}
