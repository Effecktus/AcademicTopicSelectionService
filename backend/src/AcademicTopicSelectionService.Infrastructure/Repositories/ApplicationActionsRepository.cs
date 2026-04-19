using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.ApplicationActions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Domain.Entities;
using AcademicTopicSelectionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория для работы с действиями по заявкам в PostgreSQL.
/// </summary>
/// <param name="db">Контекст базы данных.</param>
public sealed class ApplicationActionsRepository(ApplicationDbContext db) : IApplicationActionsRepository
{
    /// <inheritdoc />
    public async Task<PagedResult<ApplicationActionDto>> ListByApplicationAsync(ListApplicationActionsQuery query,
        CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var q = db.ApplicationActions.AsNoTracking()
            .Where(x => x.ApplicationId == query.ApplicationId);

        var totalCount = await q.LongCountAsync(ct);
        var items = await q
            .OrderBy(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ApplicationActionDto(
                x.Id,
                x.ApplicationId,
                x.ResponsibleId,
                x.StatusId,
                x.Status.CodeName,
                x.Status.DisplayName,
                x.Comment,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<ApplicationActionDto>(page, pageSize, totalCount, items);
    }

    /// <inheritdoc />
    public async Task<ApplicationActionDto?> GetAsync(Guid id, CancellationToken ct)
    {
        return await db.ApplicationActions.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ApplicationActionDto(
                x.Id,
                x.ApplicationId,
                x.ResponsibleId,
                x.StatusId,
                x.Status.CodeName,
                x.Status.DisplayName,
                x.Comment,
                x.CreatedAt,
                x.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public Task<bool> ApplicationExistsAsync(Guid applicationId, CancellationToken ct)
        => db.StudentApplications.AsNoTracking().AnyAsync(x => x.Id == applicationId, ct);

    /// <inheritdoc />
    public async Task<bool> UserCanReadApplicationActionsAsync(Guid applicationId, Guid userId,
        CancellationToken ct)
    {
        var viaApplication = await db.StudentApplications.AsNoTracking()
            .Where(a => a.Id == applicationId)
            .AnyAsync(
                a => a.Student.UserId == userId
                     || (a.SupervisorRequest != null && a.SupervisorRequest.TeacherUserId == userId),
                ct);

        if (viaApplication)
            return true;

        return await db.ApplicationActions.AsNoTracking()
            .AnyAsync(x => x.ApplicationId == applicationId && x.ResponsibleId == userId, ct);
    }

    /// <inheritdoc />
    public Task<bool> UserExistsAsync(Guid userId, CancellationToken ct)
        => db.Users.AsNoTracking().AnyAsync(x => x.Id == userId, ct);

    /// <inheritdoc />
    public async Task<Guid?> GetActionStatusIdByCodeNameAsync(string codeName, CancellationToken ct)
    {
        var id = await db.ApplicationActionStatuses.AsNoTracking()
            .Where(x => EF.Functions.ILike(x.CodeName, codeName))
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);

        return id;
    }

    /// <inheritdoc />
    public Task<bool> ActionStatusExistsAsync(Guid statusId, CancellationToken ct)
        => db.ApplicationActionStatuses.AsNoTracking().AnyAsync(x => x.Id == statusId, ct);

    /// <inheritdoc />
    public void Enqueue(Guid applicationId, Guid responsibleId, Guid statusId, string? comment)
    {
        db.ApplicationActions.Add(new ApplicationAction
        {
            ApplicationId = applicationId,
            ResponsibleId = responsibleId,
            StatusId = statusId,
            Comment = comment
        });
    }

    /// <inheritdoc />
    public async Task<ApplicationActionDto> CreateAsync(Guid applicationId, Guid ResponsibleId,
        Guid statusId, string? comment, CancellationToken ct)
    {
        var entity = new ApplicationAction
        {
            ApplicationId = applicationId,
            ResponsibleId = ResponsibleId,
            StatusId = statusId,
            Comment = comment
        };

        db.ApplicationActions.Add(entity);
        await db.SaveChangesAsync(ct);

        await db.Entry(entity).Reference(e => e.Status).LoadAsync(ct);

        return new ApplicationActionDto(
            entity.Id,
            entity.ApplicationId,
            entity.ResponsibleId,
            entity.StatusId,
            entity.Status.CodeName,
            entity.Status.DisplayName,
            entity.Comment,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<ApplicationActionDto?> UpdateAsync(Guid id, Guid? statusId, string? comment,
        CancellationToken ct)
    {
        var entity = await db.ApplicationActions
            .Include(x => x.Status)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null) return null;

        if (statusId is not null) entity.StatusId = statusId.Value;
        if (comment is not null) entity.Comment = comment;

        await db.SaveChangesAsync(ct);

        await db.Entry(entity).Reference(e => e.Status).LoadAsync(ct);

        return new ApplicationActionDto(
            entity.Id,
            entity.ApplicationId,
            entity.ResponsibleId,
            entity.StatusId,
            entity.Status.CodeName,
            entity.Status.DisplayName,
            entity.Comment,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.ApplicationActions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;

        db.ApplicationActions.Remove(entity);
        await db.SaveChangesAsync(ct);

        return true;
    }
}
