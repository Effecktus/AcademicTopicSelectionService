using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Teachers;

/// <inheritdoc />
public sealed class TeachersService(
    ITeachersRepository repo,
    IUsersRepository usersRepo,
    IStudentApplicationsRepository appRepo,
    IGraduateWorksRepository gwRepo) : ITeachersService
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
    public async Task<TeacherDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var dto = await repo.GetAsync(id, ct);
        if (dto is null) return null;

        var occupied = await appRepo.CountOccupiedSlotsBySupervisorAsync(dto.UserId, ct);
        return dto with { OccupiedSlotsCount = occupied };
    }

    /// <inheritdoc />
    public Task<List<TeacherGraduateWorkDto>> GetGraduateWorksAsync(Guid teacherId, CancellationToken ct)
        => gwRepo.GetByTeacherIdAsync(teacherId, ct);
}
