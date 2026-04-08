using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Students;
using AcademicTopicSelectionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Repositories;

/// <summary>
/// Реализация чтения студентов из PostgreSQL.
/// </summary>
public sealed class StudentsRepository(ApplicationDbContext db) : IStudentsRepository
{
    /// <inheritdoc />
    public async Task<PagedResult<StudentDto>> ListAsync(ListStudentsQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var baseQuery = db.Students.AsNoTracking()
            .Where(s => s.User.IsActive);

        if (query.GroupId is { } groupId)
        {
            baseQuery = baseQuery.Where(s => s.GroupId == groupId);
        }

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            var term = query.Query.Trim();
            var pattern = $"%{term}%";
            baseQuery = baseQuery.Where(s =>
                EF.Functions.ILike(s.User.Email, pattern)
                || EF.Functions.ILike(s.User.FirstName, pattern)
                || EF.Functions.ILike(s.User.LastName, pattern)
                || (s.User.MiddleName != null && EF.Functions.ILike(s.User.MiddleName, pattern)));
        }

        var totalCount = await baseQuery.LongCountAsync(ct);
        var items = await baseQuery
            .OrderBy(s => s.User.LastName)
            .ThenBy(s => s.User.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StudentDto(
                s.Id,
                s.UserId,
                s.User.Email,
                s.User.FirstName,
                s.User.LastName,
                s.User.MiddleName,
                new StudyGroupRefDto(s.Group.Id, s.Group.CodeName),
                s.CreatedAt,
                s.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<StudentDto>(page, pageSize, totalCount, items);
    }

    /// <inheritdoc />
    public async Task<StudentDto?> GetAsync(Guid id, CancellationToken ct)
    {
        return await db.Students.AsNoTracking()
            .Where(s => s.Id == id && s.User.IsActive)
            .Select(s => new StudentDto(
                s.Id,
                s.UserId,
                s.User.Email,
                s.User.FirstName,
                s.User.LastName,
                s.User.MiddleName,
                new StudyGroupRefDto(s.Group.Id, s.Group.CodeName),
                s.CreatedAt,
                s.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }
}
