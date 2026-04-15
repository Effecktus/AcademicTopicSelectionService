using System.Linq.Expressions;
using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.GraduateWorks;
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
        g.PresentationFileName);

    /// <inheritdoc />
    public async Task<PagedResult<GraduateWorkDto>> ListAsync(ListGraduateWorksQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var baseQuery = db.GraduateWorks.AsNoTracking().AsQueryable();

        if (query.Year is { } y)
            baseQuery = baseQuery.Where(g => g.Year == y);

        var total = await baseQuery.LongCountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(g => g.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ProjectToDto)
            .ToListAsync(ct);

        return new PagedResult<GraduateWorkDto>(page, pageSize, total, items);
    }

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
    public async Task<GraduateWork> AddAsync(GraduateWork entity, CancellationToken ct)
    {
        db.GraduateWorks.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
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
