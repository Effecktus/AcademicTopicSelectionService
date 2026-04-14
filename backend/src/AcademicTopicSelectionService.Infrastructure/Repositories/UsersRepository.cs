using AcademicTopicSelectionService.Application.Abstractions;
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
    public Task<Guid?> GetDepartmentHeadIdAsync(Guid departmentId, CancellationToken ct)
        => db.Departments
            .AsNoTracking()
            .Where(d => d.Id == departmentId)
            .Select(d => d.HeadId)
            .FirstOrDefaultAsync(ct);
}
