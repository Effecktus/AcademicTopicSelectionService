using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Topics;
using AcademicTopicSelectionService.Domain.Entities;
using AcademicTopicSelectionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Repositories;

/// <summary>
/// Реализация чтения тем ВКР из PostgreSQL.
/// </summary>
public sealed class TopicsRepository(ApplicationDbContext db) : ITopicsRepository
{
    /// <inheritdoc />
    public async Task<PagedResult<TopicDto>> ListAsync(ListTopicsQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var baseQuery = db.Topics.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            var term = query.Query.Trim();
            var pattern = $"%{term}%";
            baseQuery = baseQuery.Where(t =>
                EF.Functions.ILike(t.Title, pattern)
                || (t.Description != null && EF.Functions.ILike(t.Description, pattern)));
        }

        if (!string.IsNullOrWhiteSpace(query.StatusCodeName))
        {
            var statusCode = query.StatusCodeName.Trim();
            baseQuery = baseQuery.Where(t => t.Status.CodeName == statusCode);
        }

        if (query.CreatedByUserId is { } createdBy)
        {
            baseQuery = baseQuery.Where(t => t.CreatedBy == createdBy);
        }

        if (!string.IsNullOrWhiteSpace(query.CreatorTypeCodeName))
        {
            var ctCode = query.CreatorTypeCodeName.Trim();
            baseQuery = baseQuery.Where(t => t.CreatorType.CodeName == ctCode);
        }

        var totalCount = await baseQuery.LongCountAsync(ct);

        var sortKey = NormalizeSortKey(query.Sort);
        baseQuery = ApplySort(baseQuery, sortKey);

        var items = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TopicDto(
                t.Id,
                t.Title,
                t.Description,
                new DictionaryItemRefDto(t.Status.Id, t.Status.CodeName, t.Status.DisplayName),
                new DictionaryItemRefDto(t.CreatorType.Id, t.CreatorType.CodeName, t.CreatorType.DisplayName),
                t.CreatedByUser.Id,
                t.CreatedByUser.Email,
                t.CreatedByUser.FirstName,
                t.CreatedByUser.LastName,
                t.CreatedAt,
                t.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<TopicDto>(page, pageSize, totalCount, items);
    }

    /// <inheritdoc />
    public async Task<TopicDto?> GetAsync(Guid id, CancellationToken ct)
    {
        return await db.Topics.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TopicDto(
                t.Id,
                t.Title,
                t.Description,
                new DictionaryItemRefDto(t.Status.Id, t.Status.CodeName, t.Status.DisplayName),
                new DictionaryItemRefDto(t.CreatorType.Id, t.CreatorType.CodeName, t.CreatorType.DisplayName),
                t.CreatedByUser.Id,
                t.CreatedByUser.Email,
                t.CreatedByUser.FirstName,
                t.CreatedByUser.LastName,
                t.CreatedAt,
                t.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    private static string NormalizeSortKey(string? sort)
    {
        var s = (sort ?? "createdAtDesc").Replace("-", "", StringComparison.Ordinal).ToLowerInvariant();
        return s switch
        {
            "createdatdesc" or "createdatasc" or "titleasc" or "titledesc" => s,
            _ => "createdatdesc"
        };
    }

    private static IQueryable<Topic> ApplySort(IQueryable<Topic> source, string sortKey) =>
        sortKey switch
        {
            "createdatasc" => source.OrderBy(t => t.CreatedAt),
            "titleasc" => source.OrderBy(t => t.Title),
            "titledesc" => source.OrderByDescending(t => t.Title),
            _ => source.OrderByDescending(t => t.CreatedAt)
        };

    /// <inheritdoc />
    public async Task<Topic?> GetByIdForUpdateAsync(Guid id, CancellationToken ct)
    {
        return await db.Topics
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task<Topic> AddAsync(Topic topic, CancellationToken ct)
    {
        db.Topics.Add(topic);
        await db.SaveChangesAsync(ct);
        return topic;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByIdAsync(Guid id, CancellationToken ct)
    {
        return await db.Topics.AsNoTracking().AnyAsync(t => t.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task<bool> IsActiveByIdAsync(Guid id, CancellationToken ct)
    {
        return await db.Topics.AsNoTracking()
            .AnyAsync(t => t.Id == id && t.Status.CodeName == "Active", ct);
    }

    /// <inheritdoc />
    public async Task<bool> HasApplicationsAsync(Guid topicId, CancellationToken ct)
    {
        return await db.StudentApplications.AsNoTracking().AnyAsync(a => a.TopicId == topicId, ct);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var topic = await db.Topics.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (topic is not null)
        {
            db.Topics.Remove(topic);
            await db.SaveChangesAsync(ct);
        }
    }
}
