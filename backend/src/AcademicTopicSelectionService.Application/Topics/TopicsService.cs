using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Topics;

/// <inheritdoc />
/// <param name="repo">Репозиторий тем.</param>
public sealed class TopicsService(ITopicsRepository repo) : ITopicsService
{
    /// <inheritdoc />
    public Task<PagedResult<TopicDto>> ListAsync(ListTopicsQuery query, CancellationToken ct)
    {
        var normalized = query with
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 200),
            Query = string.IsNullOrWhiteSpace(query.Query) ? null : query.Query.Trim(),
            StatusCodeName = string.IsNullOrWhiteSpace(query.StatusCodeName)
                ? null
                : query.StatusCodeName.Trim(),
            CreatorTypeCodeName = string.IsNullOrWhiteSpace(query.CreatorTypeCodeName)
                ? null
                : query.CreatorTypeCodeName.Trim(),
            Sort = string.IsNullOrWhiteSpace(query.Sort) ? null : query.Sort.Trim()
        };

        return repo.ListAsync(normalized, ct);
    }

    /// <inheritdoc />
    public Task<TopicDto?> GetAsync(Guid id, CancellationToken ct) => repo.GetAsync(id, ct);
}
