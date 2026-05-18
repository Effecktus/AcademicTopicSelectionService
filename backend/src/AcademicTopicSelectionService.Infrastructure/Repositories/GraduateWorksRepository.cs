using System.Linq.Expressions;
using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.GraduateWorks;
using AcademicTopicSelectionService.Application.Teachers;
using AcademicTopicSelectionService.Domain.Entities;
using AcademicTopicSelectionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория архива ВКР.
/// </summary>
public sealed class GraduateWorksRepository(ApplicationDbContext db) : IGraduateWorksRepository
{
    private static readonly Expression<Func<GraduateWork, GraduateWorkDto>> ProjectToDto = g => new GraduateWorkDto(
        g.Id,
        g.ApplicationId,
        g.StudentId,
        g.TeacherId,
        g.Title,
        g.Year,
        g.Grade,
        g.CommissionMembers,
        g.FilePath != null,
        g.PresentationPath != null,
        g.CreatedAt,
        g.UpdatedAt,
        g.FileName,
        g.PresentationFileName,
        (g.Student.User.LastName + " " + g.Student.User.FirstName).Trim(),
        (g.Teacher.User.LastName + " " + g.Teacher.User.FirstName).Trim());

    /// <inheritdoc />
    public async Task<PagedResult<GraduateWorkDto>> ListAsync(ListGraduateWorksQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var baseQuery = db.GraduateWorks.AsNoTracking().AsQueryable();

        if (query.Year is { } y)
            baseQuery = baseQuery.Where(g => g.Year == y);

        if (!string.IsNullOrWhiteSpace(query.TitleQuery))
        {
            var term = query.TitleQuery.Trim();
            var pattern = $"%{term}%";
            baseQuery = baseQuery.Where(g => EF.Functions.ILike(g.Title, pattern));
        }

        if (query.TeacherId is { } teacherId)
            baseQuery = baseQuery.Where(g => g.TeacherId == teacherId);

        if (!string.IsNullOrWhiteSpace(query.TeacherQuery))
        {
            var teacherTerm = query.TeacherQuery.Trim();
            var teacherPattern = $"%{teacherTerm}%";
            baseQuery = baseQuery.Where(g =>
                EF.Functions.ILike(g.Teacher.User.FirstName, teacherPattern)
                || EF.Functions.ILike(g.Teacher.User.LastName, teacherPattern)
                || (g.Teacher.User.MiddleName != null && EF.Functions.ILike(g.Teacher.User.MiddleName, teacherPattern))
                || EF.Functions.ILike(
                    g.Teacher.User.LastName + " " + g.Teacher.User.FirstName, teacherPattern)
                || EF.Functions.ILike(
                    g.Teacher.User.FirstName + " " + g.Teacher.User.LastName, teacherPattern));
        }

        var total = await baseQuery.LongCountAsync(ct);

        var sortKey = NormalizeGwSortKey(query.Sort);
        var items = await ApplyGwSort(baseQuery, sortKey)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ProjectToDto)
            .ToListAsync(ct);

        return new PagedResult<GraduateWorkDto>(page, pageSize, total, items);
    }

    private static string NormalizeGwSortKey(string? sort)
    {
        var s = (sort ?? "yearDesc").Replace("-", "", StringComparison.Ordinal).ToLowerInvariant();
        return s switch
        {
            "yeardesc" or "yearasc"
                or "titledesc" or "titleasc"
                or "studentdesc" or "studentasc"
                or "teacherdesc" or "teacherasc"
                or "gradedesc" or "gradeasc" => s,
            _ => "yeardesc"
        };
    }

    private static IQueryable<GraduateWork> ApplyGwSort(IQueryable<GraduateWork> source, string sortKey) =>
        sortKey switch
        {
            "yearasc"     => source.OrderBy(g => g.Year).ThenBy(g => g.Title),
            "titleasc"    => source.OrderBy(g => g.Title),
            "titledesc"   => source.OrderByDescending(g => g.Title),
            "studentasc"  => source.OrderBy(g => g.Student.User.LastName).ThenBy(g => g.Student.User.FirstName),
            "studentdesc" => source.OrderByDescending(g => g.Student.User.LastName).ThenByDescending(g => g.Student.User.FirstName),
            "teacherasc"  => source.OrderBy(g => g.Teacher.User.LastName).ThenBy(g => g.Teacher.User.FirstName),
            "teacherdesc" => source.OrderByDescending(g => g.Teacher.User.LastName).ThenByDescending(g => g.Teacher.User.FirstName),
            "gradeasc"    => source.OrderBy(g => g.Grade),
            "gradedesc"   => source.OrderByDescending(g => g.Grade),
            _             => source.OrderByDescending(g => g.Year).ThenBy(g => g.Title),
        };

    /// <inheritdoc />
    public async Task<GraduateWorkDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await db.GraduateWorks.AsNoTracking()
            .Where(g => g.Id == id)
            .Select(ProjectToDto)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public Task<GraduateWork?> GetByIdTrackedAsync(Guid id, CancellationToken ct)
    {
        return db.GraduateWorks.FirstOrDefaultAsync(g => g.Id == id, ct);
    }

    /// <inheritdoc />
    public Task<bool> ExistsForApplicationAsync(Guid applicationId, CancellationToken ct)
    {
        return db.GraduateWorks.AsNoTracking().AnyAsync(g => g.ApplicationId == applicationId, ct);
    }

    /// <inheritdoc />
    public async Task<GraduateWorkArchiveContext?> GetArchiveContextByApplicationIdAsync(
        Guid applicationId, CancellationToken ct)
    {
        var app = await db.StudentApplications.AsNoTracking()
            .Include(a => a.SupervisorRequest)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);

        if (app?.SupervisorRequest is null)
            return null;

        var teacher = await db.Teachers.AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == app.SupervisorRequest.TeacherUserId, ct);

        if (teacher is null)
            return null;

        return new GraduateWorkArchiveContext(app.StudentId, teacher.Id);
    }

    /// <inheritdoc />
    public Task<Guid?> GetStudentUserIdByStudentProfileIdAsync(Guid studentId, CancellationToken ct)
    {
        return db.Students.AsNoTracking()
            .Where(s => s.Id == studentId)
            .Select(s => (Guid?)s.UserId)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public Task<List<TeacherGraduateWorkDto>> GetByTeacherIdAsync(Guid teacherId, CancellationToken ct)
    {
        return db.GraduateWorks.AsNoTracking()
            .Where(g => g.TeacherId == teacherId)
            .OrderByDescending(g => g.Year)
            .ThenBy(g => g.Title)
            .Select(g => new TeacherGraduateWorkDto(
                g.Id,
                g.Title,
                g.Year,
                g.Grade,
                g.Student.User.LastName,
                g.Student.User.FirstName,
                g.Student.User.MiddleName,
                g.FilePath != null,
                g.PresentationPath != null))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<GraduateWork> AddAsync(GraduateWork entity, CancellationToken ct)
    {
        db.GraduateWorks.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    /// <inheritdoc />
    public Task IncrementTeacherGraduateWorksCountAsync(Guid teacherId, CancellationToken ct)
    {
        return db.Teachers
            .Where(t => t.Id == teacherId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.GraduateWorksCount, t => t.GraduateWorksCount + 1), ct);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    /// <inheritdoc />
    public async Task DeleteAsync(GraduateWork entity, CancellationToken ct)
    {
        db.GraduateWorks.Remove(entity);
        await db.SaveChangesAsync(ct);
    }
}
