using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Teachers;
using AcademicTopicSelectionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Repositories;

/// <summary>
/// Реализация чтения преподавателей из PostgreSQL.
/// </summary>
public sealed class TeachersRepository(ApplicationDbContext db) : ITeachersRepository
{
    /// <inheritdoc />
    public async Task<PagedResult<TeacherDto>> ListAsync(ListTeachersQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var baseQuery = db.Teachers.AsNoTracking()
            .Where(t => t.User.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            var term = query.Query.Trim();
            var pattern = $"%{term}%";
            baseQuery = baseQuery.Where(t =>
                EF.Functions.ILike(t.User.Email, pattern)
                || EF.Functions.ILike(t.User.FirstName, pattern)
                || EF.Functions.ILike(t.User.LastName, pattern)
                || (t.User.MiddleName != null && EF.Functions.ILike(t.User.MiddleName, pattern)));
        }

        var totalCount = await baseQuery.LongCountAsync(ct);
        var items = await baseQuery
            .OrderBy(t => t.User.LastName)
            .ThenBy(t => t.User.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TeacherDto(
                t.Id,
                t.UserId,
                t.User.Email,
                t.User.FirstName,
                t.User.LastName,
                t.User.MiddleName,
                t.MaxStudentsLimit,
                new DictionaryItemRefDto(t.AcademicDegree.Id, t.AcademicDegree.CodeName, t.AcademicDegree.DisplayName),
                new DictionaryItemRefDto(t.AcademicTitle.Id, t.AcademicTitle.CodeName, t.AcademicTitle.DisplayName),
                new DictionaryItemRefDto(t.Position.Id, t.Position.CodeName, t.Position.DisplayName),
                t.CreatedAt,
                t.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<TeacherDto>(page, pageSize, totalCount, items);
    }

    /// <inheritdoc />
    public async Task<TeacherDto?> GetAsync(Guid id, CancellationToken ct)
    {
        return await db.Teachers.AsNoTracking()
            .Where(t => t.Id == id && t.User.IsActive)
            .Select(t => new TeacherDto(
                t.Id,
                t.UserId,
                t.User.Email,
                t.User.FirstName,
                t.User.LastName,
                t.User.MiddleName,
                t.MaxStudentsLimit,
                new DictionaryItemRefDto(t.AcademicDegree.Id, t.AcademicDegree.CodeName, t.AcademicDegree.DisplayName),
                new DictionaryItemRefDto(t.AcademicTitle.Id, t.AcademicTitle.CodeName, t.AcademicTitle.DisplayName),
                new DictionaryItemRefDto(t.Position.Id, t.Position.CodeName, t.Position.DisplayName),
                t.CreatedAt,
                t.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }
}
