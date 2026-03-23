using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.NotificationTypes;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Репозиторий для работы с типами уведомлений в базе данных.
/// </summary>
public interface INotificationTypesRepository
{
    /// <summary>
    /// Получает постраничный список типов уведомлений с возможностью поиска.
    /// </summary>
    Task<PagedResult<NotificationTypeDto>> ListAsync(ListNotificationTypesQuery query, CancellationToken ct);

    /// <summary>
    /// Получает тип уведомления по идентификатору.
    /// </summary>
    Task<NotificationTypeDto?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Проверяет существование типа уведомления с указанным именем.
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct);

    /// <summary>
    /// Создаёт новый тип уведомления.
    /// </summary>
    Task<NotificationTypeDto> CreateAsync(string name, string displayName, CancellationToken ct);

    /// <summary>
    /// Полностью обновляет тип уведомления (PUT).
    /// </summary>
    Task<NotificationTypeDto?> UpdateAsync(Guid id, string name, string displayName, CancellationToken ct);

    /// <summary>
    /// Частично обновляет тип уведомления (PATCH). Поля со значением <c>null</c> не изменяются.
    /// </summary>
    Task<NotificationTypeDto?> PatchAsync(Guid id, string? name, string? displayName, CancellationToken ct);

    /// <summary>
    /// Удаляет тип уведомления по идентификатору.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
