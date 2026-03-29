using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.ApplicationActionStatuses;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Репозиторий для работы со статусами действий по заявкам в базе данных.
/// </summary>
public interface IApplicationActionStatusesRepository
{
    /// <summary>
    /// Получает постраничный список статусов действий с возможностью поиска.
    /// </summary>
    /// <param name="query">Параметры запроса (поиск, пагинация).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Постраничный результат со списком статусов действий.</returns>
    Task<PagedResult<ApplicationActionStatusDto>> ListAsync(ListApplicationActionStatusQuery query, CancellationToken ct);

    /// <summary>
    /// Получает статус действия по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор статуса действия.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Статус действия или <c>null</c>, если не найден.</returns>
    Task<ApplicationActionStatusDto?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Проверяет существование статуса действия с указанным именем.
    /// </summary>
    /// <param name="name">Системное имя для проверки.</param>
    /// <param name="excludeId">Идентификатор записи, исключаемой из проверки (для обновления).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если статус с таким именем существует.</returns>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct);

    /// <summary>
    /// Создаёт новый статус действия.
    /// </summary>
    /// <param name="name">Системное имя.</param>
    /// <param name="displayName">Отображаемое имя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Созданный статус с присвоенным идентификатором.</returns>
    Task<ApplicationActionStatusDto> CreateAsync(string name, string displayName, CancellationToken ct);

    /// <summary>
    /// Полностью обновляет статус действия (PUT).
    /// </summary>
    /// <param name="id">Идентификатор статуса.</param>
    /// <param name="name">Новое системное имя.</param>
    /// <param name="displayName">Новое отображаемое имя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Обновлённый статус или <c>null</c>, если не найден.</returns>
    Task<ApplicationActionStatusDto?> UpdateAsync(Guid id, string name, string displayName, CancellationToken ct);

    /// <summary>
    /// Частично обновляет статус действия (PATCH). Поля со значением <c>null</c> не изменяются.
    /// </summary>
    /// <param name="id">Идентификатор статуса.</param>
    /// <param name="name">Новое системное имя или <c>null</c>.</param>
    /// <param name="displayName">Новое отображаемое имя или <c>null</c>.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Обновлённый статус или <c>null</c>, если не найден.</returns>
    Task<ApplicationActionStatusDto?> PatchAsync(Guid id, string? name, string? displayName, CancellationToken ct);

    /// <summary>
    /// Удаляет статус действия по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор статуса.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если статус был удалён; <c>false</c>, если не найден.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
