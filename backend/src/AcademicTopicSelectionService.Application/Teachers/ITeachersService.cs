using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Teachers;

/// <summary>
/// Сервис чтения преподавателей.
/// </summary>
public interface ITeachersService
{
    Task<PagedResult<TeacherDto>> ListAsync(ListTeachersQuery query, CancellationToken ct);

    Task<TeacherDto?> GetAsync(Guid id, CancellationToken ct);
}
