using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Topics;
using AcademicTopicSelectionService.Domain.Entities;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Репозиторий тем ВКР: чтение и запись.
/// </summary>
public interface ITopicsRepository
{
    Task<PagedResult<TopicDto>> ListAsync(ListTopicsQuery query, CancellationToken ct);

    Task<TopicDto?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Получить сущность темы по идентификатору (для редактирования).
    /// </summary>
    Task<Topic?> GetByIdForUpdateAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Добавить новую тему.
    /// </summary>
    Task<Topic> AddAsync(Topic topic, CancellationToken ct);

    /// <summary>
    /// Сохранить изменения tracked-сущности.
    /// </summary>
    Task SaveChangesAsync(CancellationToken ct);

    /// <summary>
    /// Проверить существование темы по идентификатору.
    /// </summary>
    Task<bool> ExistsByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Проверить, активна ли тема (Status.CodeName == "Active").
    /// </summary>
    Task<bool> IsActiveByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Проверить, что тема создана указанным пользователем.
    /// </summary>
    Task<bool> IsCreatedByUserAsync(Guid topicId, Guid userId, CancellationToken ct);

    /// <summary>
    /// Проверить, есть ли у темы заявки.
    /// </summary>
    Task<bool> HasApplicationsAsync(Guid topicId, CancellationToken ct);

    /// <summary>
    /// Удалить тему.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken ct);
}
