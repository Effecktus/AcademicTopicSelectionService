using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.AcademicTitles;
using AcademicTopicSelectionService.Infrastructure.Data;
using AcademicTopicSelectionService.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория для работы с учёными званиями в PostgreSQL.
/// </summary>
public sealed class AcademicTitlesRepository(ApplicationDbContext db) : IAcademicTitlesRepository
{
    /// <inheritdoc />
    public async Task<PagedResult<AcademicTitleDto>> ListAsync(ListAcademicTitlesQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var queryToDb = db.AcademicTitles.AsNoTracking();

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
            .Select(x => new AcademicTitleDto(x.Id, x.Name, x.DisplayName, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<AcademicTitleDto>(page, pageSize, totalCount, items);
    }

    /// <inheritdoc />
    public async Task<AcademicTitleDto?> GetAsync(Guid id, CancellationToken ct)
    {
        return await db.AcademicTitles.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AcademicTitleDto(x.Id, x.Name, x.DisplayName, x.CreatedAt, x.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct)
    {
        return await db.AcademicTitles.AsNoTracking().AnyAsync(
            x => EF.Functions.ILike(x.Name, name)
                 && (excludeId == null || x.Id != excludeId.Value),
            ct);
    }

    /// <inheritdoc />
    public async Task<AcademicTitleDto> CreateAsync(string name, string displayName, CancellationToken ct)
    {
        var entity = new AcademicTitle
        {
            Name = name,
            DisplayName = displayName
        };

        db.AcademicTitles.Add(entity);
        await db.SaveChangesAsync(ct);

        return new AcademicTitleDto(entity.Id, entity.Name, entity.DisplayName, entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<AcademicTitleDto?> UpdateAsync(Guid id, string name, string displayName, CancellationToken ct)
    {
        var entity = await db.AcademicTitles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        entity.Name = name;
        entity.DisplayName = displayName;
        await db.SaveChangesAsync(ct);

        return new AcademicTitleDto(entity.Id, entity.Name, entity.DisplayName, entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<AcademicTitleDto?> PatchAsync(Guid id, string? name, string? displayName, CancellationToken ct)
    {
        var entity = await db.AcademicTitles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        if (name is not null)
        {
            entity.Name = name;
        }

        if (displayName is not null)
        {
            entity.DisplayName = displayName;
        }

        await db.SaveChangesAsync(ct);

        return new AcademicTitleDto(entity.Id, entity.Name, entity.DisplayName, entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.AcademicTitles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return false;
        }

        db.AcademicTitles.Remove(entity);
        await db.SaveChangesAsync(ct);

        return true;
    }
}
