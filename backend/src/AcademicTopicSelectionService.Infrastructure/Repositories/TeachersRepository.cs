using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Teachers;
using AcademicTopicSelectionService.Domain.Entities;
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
        var sortKey = NormalizeSortKey(query.Sort);
        var sortedQuery = ApplySort(baseQuery, sortKey);
        var items = await sortedQuery
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

    private static string NormalizeSortKey(string? sort)
    {
        var s = (sort ?? "nameAsc").Replace("-", "", StringComparison.Ordinal).ToLowerInvariant();
        return s switch
        {
            "nameasc" or "namedesc"
                or "emailasc" or "emaildesc"
                or "academicdegreeasc" or "academicdegreedesc"
                or "academictitleasc" or "academictitledesc"
                or "positionasc" or "positiondesc"
                or "maxstudentsasc" or "maxstudentsdesc" => s,
            _ => "nameasc"
        };
    }

    private static IQueryable<Teacher> ApplySort(IQueryable<Teacher> source, string sortKey) =>
        sortKey switch
        {
            "namedesc" => source
                .OrderByDescending(t => t.User.LastName)
                .ThenByDescending(t => t.User.FirstName),
            "emailasc" => source.OrderBy(t => t.User.Email),
            "emaildesc" => source.OrderByDescending(t => t.User.Email),
            "academicdegreeasc" => source.OrderBy(t => t.AcademicDegree.DisplayName),
            "academicdegreedesc" => source.OrderByDescending(t => t.AcademicDegree.DisplayName),
            "academictitleasc" => source.OrderBy(t => t.AcademicTitle.DisplayName),
            "academictitledesc" => source.OrderByDescending(t => t.AcademicTitle.DisplayName),
            "positionasc" => source.OrderBy(t => t.Position.DisplayName),
            "positiondesc" => source.OrderByDescending(t => t.Position.DisplayName),
            "maxstudentsasc" => source.OrderBy(t => t.MaxStudentsLimit ?? int.MaxValue),
            "maxstudentsdesc" => source.OrderByDescending(t => t.MaxStudentsLimit ?? int.MinValue),
            _ => source
                .OrderBy(t => t.User.LastName)
                .ThenBy(t => t.User.FirstName)
        };

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
