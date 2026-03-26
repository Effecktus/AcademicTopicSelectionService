using AcademicTopicSelectionService.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Data;

/// <summary>
/// Реализация проверки доступности PostgreSQL через <see cref="ApplicationDbContext"/>.
/// </summary>
public sealed class DatabaseHealthChecker(ApplicationDbContext db) : IDatabaseHealthChecker
{
    /// <inheritdoc />
    public Task<bool> CanConnectAsync(CancellationToken ct)
        => db.Database.CanConnectAsync(ct);
}
