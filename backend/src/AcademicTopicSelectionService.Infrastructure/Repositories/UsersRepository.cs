using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Users;
using AcademicTopicSelectionService.Domain.Entities;
using AcademicTopicSelectionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория пользователей (PostgreSQL + EF Core).
/// </summary>
public sealed class UsersRepository(ApplicationDbContext db) : IUsersRepository
{
    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        return await db.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => EF.Functions.ILike(u.Email, email), ct);
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await db.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    /// <inheritdoc />
    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct)
    {
        return db.Users
            .AsNoTracking()
            .AnyAsync(u => EF.Functions.ILike(u.Email, email), ct);
    }

    /// <inheritdoc />
    public async Task<User> CreateAsync(User user, CancellationToken ct)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return await db.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .FirstAsync(u => u.Id == user.Id, ct);
    }

    /// <inheritdoc />
    public async Task<PagedResult<UserListItemDto>> ListAsync(ListUsersQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var q = db.Users.AsNoTracking().Include(u => u.Role).Include(u => u.Department);

        IQueryable<User> filtered = q;

        if (query.RoleId.HasValue)
            filtered = filtered.Where(u => u.RoleId == query.RoleId.Value);

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            var pattern = $"%{query.Query.Trim()}%";
            filtered = filtered.Where(u =>
                EF.Functions.ILike(u.Email, pattern)
                || EF.Functions.ILike(u.FirstName, pattern)
                || EF.Functions.ILike(u.LastName, pattern)
                || (u.MiddleName != null && EF.Functions.ILike(u.MiddleName, pattern)));
        }

        var total = await filtered.LongCountAsync(ct);
        var items = await filtered
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserListItemDto(
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.MiddleName,
                u.Role.CodeName,
                u.Role.DisplayName,
                u.DepartmentId,
                u.Department != null ? u.Department.DisplayName : null,
                u.IsActive,
                u.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<UserListItemDto>(page, pageSize, total, items);
    }

    /// <inheritdoc />
    public Task<Guid?> GetDepartmentHeadIdAsync(Guid departmentId, CancellationToken ct)
        => db.Departments
            .AsNoTracking()
            .Where(d => d.Id == departmentId)
            .Select(d => d.HeadId)
            .FirstOrDefaultAsync(ct);
}
