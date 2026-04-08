using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Topics;

/// <summary>
/// Сервис чтения тем ВКР.
/// </summary>
public interface ITopicsService
{
    Task<PagedResult<TopicDto>> ListAsync(ListTopicsQuery query, CancellationToken ct);

    Task<TopicDto?> GetAsync(Guid id, CancellationToken ct);
}
