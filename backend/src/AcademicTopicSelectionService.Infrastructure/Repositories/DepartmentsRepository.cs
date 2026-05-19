using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Departments;
using AcademicTopicSelectionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Repositories;

/// <summary>
/// Реализация чтения справочника кафедр из PostgreSQL.
/// </summary>
public sealed class DepartmentsRepository(ApplicationDbContext db) : IDepartmentsRepository
{
    /// <inheritdoc />
    public Task<List<DepartmentDto>> GetAllAsync(CancellationToken ct)
        => db.Departments
            .AsNoTracking()
            .OrderBy(d => d.DisplayName)
            .Select(d => new DepartmentDto(d.Id, d.CodeName, d.DisplayName))
            .ToListAsync(ct);
}
