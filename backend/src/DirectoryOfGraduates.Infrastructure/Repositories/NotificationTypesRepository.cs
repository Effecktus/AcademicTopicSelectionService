using DirectoryOfGraduates.Application.Abstractions;
using DirectoryOfGraduates.Application.Dictionaries;
using DirectoryOfGraduates.Application.Dictionaries.NotificationTypes;
using DirectoryOfGraduates.Infrastructure.Data;
using DirectoryOfGraduates.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DirectoryOfGraduates.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория для работы с типами уведомлений в PostgreSQL.
/// </summary>
public sealed class NotificationTypesRepository(ApplicationDbContext db) : INotificationTypesRepository
{
    /// <inheritdoc />
    public async Task<PagedResult<NotificationTypeDto>> ListAsync(ListNotificationTypesQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var queryToDb = db.NotificationTypes.AsNoTracking();

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
            .Select(x => new NotificationTypeDto(x.Id, x.Name, x.DisplayName, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<NotificationTypeDto>(page, pageSize, totalCount, items);
    }

    /// <inheritdoc />
    public async Task<NotificationTypeDto?> GetAsync(Guid id, CancellationToken ct)
    {
        return await db.NotificationTypes.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new NotificationTypeDto(x.Id, x.Name, x.DisplayName, x.CreatedAt, x.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct)
    {
        return await db.NotificationTypes.AsNoTracking().AnyAsync(
            x => EF.Functions.ILike(x.Name, name)
                 && (excludeId == null || x.Id != excludeId.Value),
            ct);
    }

    /// <inheritdoc />
    public async Task<NotificationTypeDto> CreateAsync(string name, string displayName, CancellationToken ct)
    {
        var entity = new NotificationType
        {
            Name = name,
            DisplayName = displayName
        };

        db.NotificationTypes.Add(entity);
        await db.SaveChangesAsync(ct);

        return new NotificationTypeDto(entity.Id, entity.Name, entity.DisplayName, entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<NotificationTypeDto?> UpdateAsync(Guid id, string name, string displayName, CancellationToken ct)
    {
        var entity = await db.NotificationTypes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        entity.Name = name;
        entity.DisplayName = displayName;
        await db.SaveChangesAsync(ct);

        return new NotificationTypeDto(entity.Id, entity.Name, entity.DisplayName, entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<NotificationTypeDto?> PatchAsync(Guid id, string? name, string? displayName, CancellationToken ct)
    {
        var entity = await db.NotificationTypes.FirstOrDefaultAsync(x => x.Id == id, ct);
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

        return new NotificationTypeDto(entity.Id, entity.Name, entity.DisplayName, entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.NotificationTypes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return false;
        }

        db.NotificationTypes.Remove(entity);
        await db.SaveChangesAsync(ct);

        return true;
    }
}
