using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Teachers;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Чтение данных о преподавателях из БД.
/// </summary>
public interface ITeachersRepository
{
    Task<PagedResult<TeacherDto>> ListAsync(ListTeachersQuery query, CancellationToken ct);

    Task<TeacherDto?> GetAsync(Guid id, CancellationToken ct);
}
