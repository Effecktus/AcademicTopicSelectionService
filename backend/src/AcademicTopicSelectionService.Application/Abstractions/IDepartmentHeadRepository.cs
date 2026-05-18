using AcademicTopicSelectionService.Application.DepartmentHead;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Агрегирующие запросы для страницы аналитики заведующего кафедрой.
/// </summary>
public interface IDepartmentHeadRepository
{
    Task<DepartmentHeadAnalyticsDto?> GetAnalyticsAsync(Guid departmentHeadUserId, CancellationToken ct);
}
