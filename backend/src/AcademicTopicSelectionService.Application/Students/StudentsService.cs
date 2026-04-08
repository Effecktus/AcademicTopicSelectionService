using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Students;

/// <inheritdoc />
/// <param name="repo">Репозиторий студентов.</param>
public sealed class StudentsService(IStudentsRepository repo) : IStudentsService
{
    /// <inheritdoc />
    public Task<PagedResult<StudentDto>> ListAsync(ListStudentsQuery query, CancellationToken ct)
    {
        var normalized = query with
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 200),
            Query = string.IsNullOrWhiteSpace(query.Query) ? null : query.Query.Trim()
        };

        return repo.ListAsync(normalized, ct);
    }

    /// <inheritdoc />
    public Task<StudentDto?> GetAsync(Guid id, CancellationToken ct) => repo.GetAsync(id, ct);
}
