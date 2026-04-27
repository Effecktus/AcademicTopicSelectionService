using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Teachers;

/// <inheritdoc />
/// <param name="repo">Репозиторий преподавателей.</param>
public sealed class TeachersService(ITeachersRepository repo) : ITeachersService
{
    /// <inheritdoc />
    public Task<PagedResult<TeacherDto>> ListAsync(ListTeachersQuery query, CancellationToken ct)
    {
        var normalized = query with
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 200),
            Query = string.IsNullOrWhiteSpace(query.Query) ? null : query.Query.Trim(),
            Sort = string.IsNullOrWhiteSpace(query.Sort) ? null : query.Sort.Trim()
        };

        return repo.ListAsync(normalized, ct);
    }

    /// <inheritdoc />
    public Task<TeacherDto?> GetAsync(Guid id, CancellationToken ct) => repo.GetAsync(id, ct);
}
