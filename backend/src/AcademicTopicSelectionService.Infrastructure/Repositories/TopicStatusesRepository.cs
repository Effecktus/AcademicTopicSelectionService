using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.TopicStatuses;
using AcademicTopicSelectionService.Infrastructure.Data;
using AcademicTopicSelectionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория для работы со статусами тем ВКР в PostgreSQL.
/// </summary>
/// <param name="db">Контекст базы данных.</param>
public sealed class TopicStatusesRepository(ApplicationDbContext db) : ITopicStatusesRepository
{
    /// <inheritdoc />
    public async Task<PagedResult<TopicStatusDto>> ListAsync(ListTopicStatusesQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var queryToDb = db.TopicStatuses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            var term = query.Query.Trim();
            queryToDb = queryToDb.Where(x => EF.Functions.ILike(x.CodeName, $"%{term}%")
                                             || EF.Functions.ILike(x.DisplayName, $"%{term}%"));
        }

        var totalCount = await queryToDb.LongCountAsync(ct);
        var items = await queryToDb
            .OrderBy(x => x.CodeName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TopicStatusDto(x.Id, x.CodeName, x.DisplayName, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<TopicStatusDto>(page, pageSize, totalCount, items);
    }

    /// <inheritdoc />
    public async Task<TopicStatusDto?> GetAsync(Guid id, CancellationToken ct)
    {
        return await db.TopicStatuses.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new TopicStatusDto(x.Id, x.CodeName, x.DisplayName, x.CreatedAt, x.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct)
    {
        return await db.TopicStatuses.AsNoTracking().AnyAsync(
            x => EF.Functions.ILike(x.CodeName, name)
                 && (excludeId == null || x.Id != excludeId.Value),
            ct);
    }

    /// <inheritdoc />
    public async Task<TopicStatusDto> CreateAsync(string name, string displayName, CancellationToken ct)
    {
        var entity = new TopicStatus
        {
            CodeName = name,
            DisplayName = displayName
        };

        db.TopicStatuses.Add(entity);
        await db.SaveChangesAsync(ct);

        return new TopicStatusDto(entity.Id, entity.CodeName, entity.DisplayName, entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<TopicStatusDto?> UpdateAsync(Guid id, string name, string displayName, CancellationToken ct)
    {
        var entity = await db.TopicStatuses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        entity.CodeName = name;
        entity.DisplayName = displayName;
        await db.SaveChangesAsync(ct);

        return new TopicStatusDto(entity.Id, entity.CodeName, entity.DisplayName, entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<TopicStatusDto?> PatchAsync(Guid id, string? name, string? displayName, CancellationToken ct)
    {
        var entity = await db.TopicStatuses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        if (name is not null)
        {
            entity.CodeName = name;
        }

        if (displayName is not null)
        {
            entity.DisplayName = displayName;
        }

        await db.SaveChangesAsync(ct);

        return new TopicStatusDto(entity.Id, entity.CodeName, entity.DisplayName, entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.TopicStatuses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return false;
        }

        db.TopicStatuses.Remove(entity);
        await db.SaveChangesAsync(ct);

        return true;
    }
}
