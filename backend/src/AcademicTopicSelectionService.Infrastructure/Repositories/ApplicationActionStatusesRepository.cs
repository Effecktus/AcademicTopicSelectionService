using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.ApplicationActionStatuses;
using AcademicTopicSelectionService.Domain.Entities;
using AcademicTopicSelectionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория для работы со статусами действий по заявкам в PostgreSQL.
/// </summary>
/// <param name="db">Контекст базы данных.</param>
public sealed class ApplicationActionStatusesRepository(ApplicationDbContext db)
    : IApplicationActionStatusesRepository
{
    /// <inheritdoc />
    public async Task<PagedResult<ApplicationActionStatusDto>> ListAsync(ListApplicationActionStatusQuery query,
        CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var q = db.ApplicationActionStatuses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            var term = query.Query.Trim();
            q = q.Where(x => EF.Functions.ILike(x.CodeName, $"%{term}%")
                              || EF.Functions.ILike(x.DisplayName, $"%{term}%"));
        }

        var totalCount = await q.LongCountAsync(ct);
        var items = await q
            .OrderBy(x => x.CodeName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ApplicationActionStatusDto(x.Id, x.CodeName, x.DisplayName, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<ApplicationActionStatusDto>(page, pageSize, totalCount, items);
    }

    /// <inheritdoc />
    public async Task<ApplicationActionStatusDto?> GetAsync(Guid id, CancellationToken ct)
    {
        return await db.ApplicationActionStatuses.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ApplicationActionStatusDto(x.Id, x.CodeName, x.DisplayName, x.CreatedAt, x.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct)
    {
        return await db.ApplicationActionStatuses.AsNoTracking()
            .AnyAsync(x => EF.Functions.ILike(x.CodeName, name)
                           && (excludeId == null || x.Id != excludeId.Value), ct);
    }

    /// <inheritdoc />
    public async Task<ApplicationActionStatusDto> CreateAsync(string name, string displayName, CancellationToken ct)
    {
        var entity = new ApplicationActionStatus
        {
            CodeName = name,
            DisplayName = displayName
        };

        db.ApplicationActionStatuses.Add(entity);
        await db.SaveChangesAsync(ct);

        return new ApplicationActionStatusDto(entity.Id, entity.CodeName, entity.DisplayName,
            entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<ApplicationActionStatusDto?> UpdateAsync(Guid id, string name, string displayName,
        CancellationToken ct)
    {
        var entity = await db.ApplicationActionStatuses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return null;

        entity.CodeName = name;
        entity.DisplayName = displayName;
        await db.SaveChangesAsync(ct);

        return new ApplicationActionStatusDto(entity.Id, entity.CodeName, entity.DisplayName,
            entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<ApplicationActionStatusDto?> PatchAsync(Guid id, string? name, string? displayName,
        CancellationToken ct)
    {
        var entity = await db.ApplicationActionStatuses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return null;

        if (name is not null) entity.CodeName = name;
        if (displayName is not null) entity.DisplayName = displayName;

        await db.SaveChangesAsync(ct);

        return new ApplicationActionStatusDto(entity.Id, entity.CodeName, entity.DisplayName,
            entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.ApplicationActionStatuses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;

        db.ApplicationActionStatuses.Remove(entity);
        await db.SaveChangesAsync(ct);

        return true;
    }
}
