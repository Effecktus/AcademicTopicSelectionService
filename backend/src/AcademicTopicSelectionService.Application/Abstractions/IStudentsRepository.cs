using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Students;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Чтение данных о студентах из БД.
/// </summary>
public interface IStudentsRepository
{
    Task<PagedResult<StudentDto>> ListAsync(ListStudentsQuery query, CancellationToken ct);

    Task<StudentDto?> GetAsync(Guid id, CancellationToken ct);
}
