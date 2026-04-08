using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Students;

/// <summary>
/// Сервис чтения студентов.
/// </summary>
public interface IStudentsService
{
    Task<PagedResult<StudentDto>> ListAsync(ListStudentsQuery query, CancellationToken ct);

    Task<StudentDto?> GetAsync(Guid id, CancellationToken ct);
}
