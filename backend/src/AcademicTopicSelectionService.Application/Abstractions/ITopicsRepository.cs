using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Topics;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Чтение тем ВКР из БД.
/// </summary>
public interface ITopicsRepository
{
    Task<PagedResult<TopicDto>> ListAsync(ListTopicsQuery query, CancellationToken ct);

    Task<TopicDto?> GetAsync(Guid id, CancellationToken ct);
}
