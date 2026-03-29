using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.StudyGroups;
using AcademicTopicSelectionService.Domain.Entities;
using AcademicTopicSelectionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория для работы с учебными группами в PostgreSQL.
/// </summary>
/// <param name="db">Контекст базы данных.</param>
public sealed class StudyGroupsRepository(ApplicationDbContext db) : IStudyGroupsRepository
{
    /// <inheritdoc />
    public async Task<PagedResult<StudyGroupDto>> ListAsync(ListStudyGroupsQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var queryToDb = db.StudyGroups.AsNoTracking();

        if (query.CodeName.HasValue)
        {
            queryToDb = queryToDb.Where(x => x.CodeName == query.CodeName.Value);
        }

        var totalCount = await queryToDb.LongCountAsync(ct);
        var items = await queryToDb
            .OrderBy(x => x.CodeName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new StudyGroupDto(x.Id, x.CodeName, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<StudyGroupDto>(page, pageSize, totalCount, items);
    }

    /// <inheritdoc />
    public async Task<StudyGroupDto?> GetAsync(Guid id, CancellationToken ct)
    {
        return await db.StudyGroups.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new StudyGroupDto(x.Id, x.CodeName, x.CreatedAt, x.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public Task<bool> ExistsByCodeNameAsync(int codeName, Guid? excludeId, CancellationToken ct)
    {
        return db.StudyGroups.AsNoTracking().AnyAsync(
            x => x.CodeName == codeName
                 && (excludeId == null || x.Id != excludeId.Value),
            ct);
    }

    /// <inheritdoc />
    public async Task<StudyGroupDto> CreateAsync(int codeName, CancellationToken ct)
    {
        var entity = new StudyGroup { CodeName = codeName };

        db.StudyGroups.Add(entity);
        await db.SaveChangesAsync(ct);

        return new StudyGroupDto(entity.Id, entity.CodeName, entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<StudyGroupDto?> UpdateAsync(Guid id, int codeName, CancellationToken ct)
    {
        var entity = await db.StudyGroups.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        entity.CodeName = codeName;
        await db.SaveChangesAsync(ct);

        return new StudyGroupDto(entity.Id, entity.CodeName, entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<StudyGroupDto?> PatchAsync(Guid id, int? codeName, CancellationToken ct)
    {
        var entity = await db.StudyGroups.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        if (codeName.HasValue)
        {
            entity.CodeName = codeName.Value;
        }

        await db.SaveChangesAsync(ct);

        return new StudyGroupDto(entity.Id, entity.CodeName, entity.CreatedAt, entity.UpdatedAt);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.StudyGroups.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
        {
            return false;
        }

        db.StudyGroups.Remove(entity);
        await db.SaveChangesAsync(ct);

        return true;
    }
}
