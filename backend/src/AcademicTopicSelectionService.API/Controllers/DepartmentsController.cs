using Asp.Versioning;
using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Departments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// Справочник кафедр.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/departments")]
[Produces("application/json")]
[Authorize]
public sealed class DepartmentsController(IDepartmentsRepository repo) : ControllerBase
{
    /// <summary>
    /// Список всех кафедр.
    /// </summary>
    [ProducesResponseType(typeof(List<DepartmentDto>), StatusCodes.Status200OK)]
    [HttpGet]
    public async Task<ActionResult<List<DepartmentDto>>> ListAsync(CancellationToken ct = default)
    {
        var departments = await repo.GetAllAsync(ct);
        return Ok(departments);
    }
}
