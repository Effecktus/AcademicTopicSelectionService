using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.AcademicDegrees;
using AcademicTopicSelectionService.Infrastructure.Data;
using AcademicTopicSelectionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория для работы с учёными степенями в PostgreSQL.
/// </summary>
public sealed class AcademicDegreesRepository(ApplicationDbContext db) : IAcademicDegreesRepository
{
    /// <inheritdoc />
    public async Task<PagedResult<AcademicDegreeDto>> ListAsync(ListAcademicDegreesQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var queryToDb = db.AcademicDegrees.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            var term = query.Query.Trim();
            queryToDb = queryToDb.Where(x => EF.Functions.ILike(x.Name, $"%{term}%")
                                             || EF.Functions.ILike(x.DisplayName, $"%{term}%")
                                             || (x.ShortName != null && EF.Functions.ILike(x.ShortName, $"%{term}%")));
        }

        var totalCount = await queryToDb.LongCountAsync(ct);
        var items = await queryToDb
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AcademicDegreeDto(x.Id, x.Name, x.DisplayName, x.ShortName, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<AcademicDegreeDto>(page, pageSize, totalCount, items);
    }

    /// <inheritdoc />
    public async Task<AcademicDegreeDto?> GetAsync(Guid id, CancellationToken ct)
    {
        return await db.AcademicDegrees.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AcademicDegreeDto(x.Id, x.Name, x.DisplayName, x.ShortName, x.CreatedAt, x.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct)
    {
        return await db.AcademicDegrees.AsNoTracking().AnyAsync(
            x => EF.Functions.ILike(x.Name, name)
                 && (excludeId == null || x.Id != excludeId.Value),
            ct);
    }

    /// <inheritdoc />
    public async Task<AcademicDegreeDto> CreateAsync(string name, string displayName, string? shortName, CancellationToken ct)
    {
        var entity = new AcademicDegree
        {
            Name = name,
            DisplayName = displayName,
            ShortName = shortName
        };

        db.AcademicDegrees.Add(entity);
        await db.SaveChangesAsync(ct);

        return new AcademicDegreeDto(entity.Id, entity.Name, entity.DisplayName, entity.ShortName, entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<AcademicDegreeDto?> UpdateAsync(Guid id, string name, string displayName, string? shortName, CancellationToken ct)
    {
        var entity = await db.AcademicDegrees.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        entity.Name = name;
        entity.DisplayName = displayName;
        entity.ShortName = shortName;
        await db.SaveChangesAsync(ct);

        return new AcademicDegreeDto(entity.Id, entity.Name, entity.DisplayName, entity.ShortName, entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<AcademicDegreeDto?> PatchAsync(Guid id, string? name, string? displayName, string? shortName, CancellationToken ct)
    {
        var entity = await db.AcademicDegrees.FirstOrDefaultAsync(x => x.Id == id, ct);
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

        if (shortName is not null)
        {
            entity.ShortName = shortName.Length == 0 ? null : shortName;
        }

        await db.SaveChangesAsync(ct);

        return new AcademicDegreeDto(entity.Id, entity.Name, entity.DisplayName, entity.ShortName, entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.AcademicDegrees.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return false;
        }

        db.AcademicDegrees.Remove(entity);
        await db.SaveChangesAsync(ct);

        return true;
    }
}
