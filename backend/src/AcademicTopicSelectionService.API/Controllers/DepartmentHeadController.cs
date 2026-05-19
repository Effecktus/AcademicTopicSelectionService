using Asp.Versioning;
using AcademicTopicSelectionService.API.Authorization;
using AcademicTopicSelectionService.API.Extensions;
using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.DepartmentHead;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// Эндпоинты для заведующего кафедрой.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/department-head")]
[Produces("application/json")]
[Authorize(Roles = AppRoles.DepartmentHead)]
public sealed class DepartmentHeadController(IDepartmentHeadRepository repo) : ControllerBase
{
    /// <summary>
    /// Аналитика по кафедре текущего заведующего кафедрой.
    /// </summary>
    [ProducesResponseType(typeof(DepartmentHeadAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [HttpGet("analytics")]
    public async Task<ActionResult<DepartmentHeadAnalyticsDto>> GetAnalyticsAsync(CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Problem(title: "Unauthorized", detail: "User ID not found in token",
                statusCode: StatusCodes.Status401Unauthorized);

        var result = await repo.GetAnalyticsAsync(userId.Value, ct);
        if (result is null)
            return Problem(title: "Not Found", detail: "Department not configured for this user",
                statusCode: StatusCodes.Status404NotFound);

        return Ok(result);
    }
}
