namespace AcademicTopicSelectionService.Application.Dictionaries.NotificationTypes;

/// <summary>
/// Сервис бизнес-логики для работы с типами уведомлений.
/// Выполняет валидацию, проверку на уникальность и делегирует операции репозиторию.
/// </summary>
public interface INotificationTypesService
{
    /// <summary>
    /// Получает постраничный список типов уведомлений с нормализацией параметров запроса.
    /// </summary>
    Task<PagedResult<NotificationTypeDto>> ListAsync(ListNotificationTypesQuery query, CancellationToken ct);

    /// <summary>
    /// Получает тип уведомления по идентификатору.
    /// </summary>
    Task<NotificationTypeDto?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Создаёт новый тип уведомления с валидацией и проверкой уникальности имени.
    /// </summary>
    Task<Result<NotificationTypeDto, NotificationTypesError>> CreateAsync(
        UpsertNotificationTypeCommand command, CancellationToken ct);

    /// <summary>
    /// Полностью обновляет тип уведомления (PUT) с валидацией и проверкой уникальности имени.
    /// </summary>
    Task<Result<NotificationTypeDto, NotificationTypesError>> UpdateAsync(
        Guid id, UpsertNotificationTypeCommand command, CancellationToken ct);

    /// <summary>
    /// Частично обновляет тип уведомления (PATCH). Поля со значением <c>null</c> не изменяются.
    /// </summary>
    Task<Result<NotificationTypeDto, NotificationTypesError>> PatchAsync(
        Guid id, UpsertNotificationTypeCommand command, CancellationToken ct);

    /// <summary>
    /// Удаляет тип уведомления по идентификатору.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
