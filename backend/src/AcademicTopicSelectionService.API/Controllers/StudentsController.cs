using Asp.Versioning;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Students;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// Чтение каталога студентов.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/students")]
[Produces("application/json")]
[Authorize]
public sealed class StudentsController(IStudentsService service) : ControllerBase
{
    /// <summary>
    /// Список студентов (только активные пользователи).
    /// </summary>
    [ProducesResponseType(typeof(PagedResult<StudentDto>), StatusCodes.Status200OK)]
    [HttpGet]
    public async Task<ActionResult<PagedResult<StudentDto>>> ListAsync(
        [FromQuery] string? query,
        [FromQuery] Guid? groupId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await service.ListAsync(new ListStudentsQuery(query, groupId, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Студент по идентификатору записи <c>Students.Id</c>.
    /// </summary>
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StudentDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var student = await service.GetAsync(id, ct);
        return student is null
            ? Problem(title: "Not Found", detail: "Student not found", statusCode: StatusCodes.Status404NotFound,
                instance: id.ToString())
            : Ok(student);
    }
}
