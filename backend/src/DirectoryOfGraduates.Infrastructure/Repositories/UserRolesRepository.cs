using DirectoryOfGraduates.Application.Abstractions;
using DirectoryOfGraduates.Application.Dictionaries.UserRoles;
using DirectoryOfGraduates.Infrastructure.Data;
using DirectoryOfGraduates.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DirectoryOfGraduates.Infrastructure.Repositories;

public sealed class UserRolesRepository : IUserRolesRepository
{
    private readonly ApplicationDbContext _db;

    public UserRolesRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<UserRoleDto>> ListAsync(ListUserRolesQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        IQueryable<UserRole> q = _db.UserRoles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim();
            q = q.Where(x => EF.Functions.ILike(x.Name, $"%{term}%")
                          || EF.Functions.ILike(x.DisplayName, $"%{term}%"));
        }

        var total = await q.LongCountAsync(ct);
        var items = await q
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserRoleDto(x.Id, x.Name, x.DisplayName, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<UserRoleDto>(page, pageSize, total, items);
    }

    public async Task<UserRoleDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.UserRoles.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new UserRoleDto(x.Id, x.Name, x.DisplayName, x.CreatedAt, x.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    public Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct)
    {
        return _db.UserRoles.AsNoTracking().AnyAsync(
            x => x.Name == name && (excludeId == null || x.Id != excludeId.Value),
            ct);
    }

    public async Task<UserRoleDto> CreateAsync(string name, string displayName, CancellationToken ct)
    {
        var entity = new UserRole
        {
            Name = name,
            DisplayName = displayName
        };

        _db.UserRoles.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new UserRoleDto(entity.Id, entity.Name, entity.DisplayName, entity.CreatedAt, entity.UpdatedAt);
    }

    public async Task<UserRoleDto?> UpdateAsync(Guid id, string name, string displayName, CancellationToken ct)
    {
        var entity = await _db.UserRoles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return null;

        entity.Name = name;
        entity.DisplayName = displayName;
        await _db.SaveChangesAsync(ct);

        return new UserRoleDto(entity.Id, entity.Name, entity.DisplayName, entity.CreatedAt, entity.UpdatedAt);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.UserRoles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;

        _db.UserRoles.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

