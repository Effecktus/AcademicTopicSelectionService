using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Teachers;

/// <inheritdoc />
/// <param name="repo">Репозиторий преподавателей.</param>
/// <param name="usersRepo">Репозиторий пользователей.</param>
public sealed class TeachersService(ITeachersRepository repo, IUsersRepository usersRepo) : ITeachersService
{
    /// <inheritdoc />
    public async Task<PagedResult<TeacherDto>> ListAsync(
        ListTeachersQuery query,
        string roleCodeName,
        Guid userId,
        CancellationToken ct)
    {
        Guid? departmentId = null;
        if (string.Equals(roleCodeName, "Student", StringComparison.Ordinal))
        {
            var user = await usersRepo.GetByIdAsync(userId, ct);
            departmentId = user?.DepartmentId;
        }

        var normalized = query with
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 200),
            Query = string.IsNullOrWhiteSpace(query.Query) ? null : query.Query.Trim(),
            Sort = string.IsNullOrWhiteSpace(query.Sort) ? null : query.Sort.Trim(),
            DepartmentId = departmentId
        };

        return await repo.ListAsync(normalized, ct);
    }

    /// <inheritdoc />
    public Task<TeacherDto?> GetAsync(Guid id, CancellationToken ct) => repo.GetAsync(id, ct);
}
