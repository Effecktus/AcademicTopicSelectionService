using DirectoryOfGraduates.Application.Abstractions;
using DirectoryOfGraduates.Application.Dictionaries;
using DirectoryOfGraduates.Application.Dictionaries.Positions;
using DirectoryOfGraduates.Infrastructure.Data;
using DirectoryOfGraduates.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DirectoryOfGraduates.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория для работы с должностями в PostgreSQL.
/// </summary>
/// <param name="db">Контекст базы данных.</param>
public sealed class PositionsRepository(ApplicationDbContext db) : IPositionsRepository
{
    /// <inheritdoc />
    public async Task<PagedResult<PositionDto>> ListAsync(ListPositionsQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var queryToDb = db.Positions.AsNoTracking();

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
            .Select(x => new PositionDto(x.Id, x.Name, x.DisplayName, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<PositionDto>(page, pageSize, totalCount, items);
    }

    /// <inheritdoc />
    public async Task<PositionDto?> GetAsync(Guid id, CancellationToken ct)
    {
        return await db.Positions.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new PositionDto(x.Id, x.Name, x.DisplayName, x.CreatedAt, x.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct)
    {
        return db.Positions.AsNoTracking().AnyAsync(
            x => EF.Functions.ILike(x.Name, name)
                 && (excludeId == null || x.Id != excludeId.Value),
            ct);
    }

    /// <inheritdoc />
    public async Task<PositionDto> CreateAsync(string name, string displayName, CancellationToken ct)
    {
        var entity = new Position
        {
            Name = name,
            DisplayName = displayName
        };

        db.Positions.Add(entity);
        await db.SaveChangesAsync(ct);

        return new PositionDto(entity.Id, entity.Name, entity.DisplayName, entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<PositionDto?> UpdateAsync(Guid id, string name, string displayName, CancellationToken ct)
    {
        var entity = await db.Positions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        entity.Name = name;
        entity.DisplayName = displayName;
        await db.SaveChangesAsync(ct);

        return new PositionDto(entity.Id, entity.Name, entity.DisplayName, entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<PositionDto?> PatchAsync(Guid id, string? name, string? displayName, CancellationToken ct)
    {
        var entity = await db.Positions.FirstOrDefaultAsync(x => x.Id == id, ct);
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

        return new PositionDto(entity.Id, entity.Name, entity.DisplayName, entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.Positions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return false;
        }

        db.Positions.Remove(entity);
        await db.SaveChangesAsync(ct);

        return true;
    }
}
