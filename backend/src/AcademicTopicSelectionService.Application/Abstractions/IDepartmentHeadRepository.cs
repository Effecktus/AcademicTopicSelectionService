using AcademicTopicSelectionService.Application.DepartmentHead;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Агрегирующие запросы для страницы аналитики заведующего кафедрой.
/// </summary>
public interface IDepartmentHeadRepository
{
    /// <summary>
    /// Возвращает аналитику по кафедре заведующего.
    /// Если <paramref name="year"/> указан, все агрегаты фильтруются по этому году.
    /// </summary>
    Task<DepartmentHeadAnalyticsDto?> GetAnalyticsAsync(Guid departmentHeadUserId, int? year, CancellationToken ct);
}
