using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.ApplicationStatuses;
using AcademicTopicSelectionService.Infrastructure.Data;
using AcademicTopicSelectionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория для работы со статусами заявки в PostgreSQL.
/// </summary>
/// <param name="db">Контекст базы данных.</param>
public sealed class ApplicationStatusesRepository(ApplicationDbContext db) : IApplicationStatusesRepository
{
    /// <inheritdoc/>>
    public async Task<PagedResult<ApplicationStatusDto>> ListAsync(ListApplicationStatusQuery query,
        CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var queryToDb = db.ApplicationStatuses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            var term = query.Query.Trim();
            queryToDb = queryToDb.Where(x => EF.Functions.ILike(x.Name, $"%{term}%")
                                             || EF.Functions.ILike(x.DisplayName, $"%{term}%"));
        }

        var totalCount = await queryToDb.LongCountAsync(ct);
        var items = await queryToDb
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ApplicationStatusDto(x.Id, x.Name, x.DisplayName, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<ApplicationStatusDto>(page, pageSize, totalCount, items);
    }

    /// <inheritdoc />
    public async Task<ApplicationStatusDto?> GetAsync(Guid id, CancellationToken ct)
    {
        return await db.ApplicationStatuses.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ApplicationStatusDto(x.Id, x.Name, x.DisplayName, x.CreatedAt, x.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct)
    {
        return await db.ApplicationStatuses.AsNoTracking() .AnyAsync(
            x => EF.Functions.ILike(x.Name, name)
                 && (excludeId == null || x.Id != excludeId.Value),
            ct);
    }

    /// <inheritdoc/>
    public async Task<ApplicationStatusDto> CreateAsync(string name, string displayName, CancellationToken ct)
    {
        var entity = new ApplicationStatus
        {
            Name = name,
            DisplayName = displayName
        };
        
        db.ApplicationStatuses.Add(entity);
        await db.SaveChangesAsync(ct);
        
        return new ApplicationStatusDto(entity.Id, entity.Name, entity.DisplayName, entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc/>
    public async Task<ApplicationStatusDto?> UpdateAsync(Guid id, string name, string displayName, CancellationToken ct)
    {
        var entity = await db.ApplicationStatuses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return null;
        }
        
        entity.Name = name;
        entity.DisplayName = displayName;
        await db.SaveChangesAsync(ct);
        
        return new ApplicationStatusDto(entity.Id, entity.Name, entity.DisplayName, entity.CreatedAt, entity.UpdatedAt);
    }
    
    /// <inheritdoc />
    public async Task<ApplicationStatusDto?> PatchAsync(Guid id, string? name, string? displayName,
        CancellationToken ct)
    {
        var entity = await db.ApplicationStatuses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        if (name is not null)
        {
            entity.Name = name;
        }

        if (displayName is  not null)
        {
            entity.DisplayName = displayName;
        }
        
        await db.SaveChangesAsync(ct);

        return new ApplicationStatusDto(entity.Id, entity.Name, entity.DisplayName, entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.ApplicationStatuses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return false;
        }

        db.ApplicationStatuses.Remove(entity);
        await db.SaveChangesAsync(ct);

        return true;
    }
}