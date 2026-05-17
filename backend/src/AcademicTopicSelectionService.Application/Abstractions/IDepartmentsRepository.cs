using AcademicTopicSelectionService.Application.Departments;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Доступ к справочнику кафедр.
/// </summary>
public interface IDepartmentsRepository
{
    /// <summary>
    /// Возвращает все кафедры, упорядоченные по отображаемому имени.
    /// </summary>
    Task<List<DepartmentDto>> GetAllAsync(CancellationToken ct);
}
