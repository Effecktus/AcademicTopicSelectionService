using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.TopicStatuses;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Репозиторий для работы со статусами тем ВКР в базе данных.
/// </summary>
public interface ITopicStatusesRepository
{
    /// <summary>
    /// Получает постраничный список статусов темы с возможностью поиска.
    /// </summary>
    /// <param name="query">Параметры запроса (поиск, пагинация).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Постраничный результат со списком статусов темы.</returns>
    Task<PagedResult<TopicStatusDto>> ListAsync(ListTopicStatusesQuery query, CancellationToken ct);

    /// <summary>
    /// Получает статус темы по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор статуса темы.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Статус темы или <c>null</c>, если не найден.</returns>
    Task<TopicStatusDto?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Проверяет существование статуса темы с указанным именем.
    /// </summary>
    /// <param name="name">Системное имя для проверки.</param>
    /// <param name="excludeId">Идентификатор записи, которую нужно исключить из проверки (для обновления).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если статус темы с таким именем существует.</returns>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct);

    /// <summary>
    /// Создаёт новый статус темы.
    /// </summary>
    /// <param name="name">Системное имя.</param>
    /// <param name="displayName">Отображаемое имя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Созданный статус темы с присвоенным идентификатором.</returns>
    Task<TopicStatusDto> CreateAsync(string name, string displayName, CancellationToken ct);

    /// <summary>
    /// Полностью обновляет статус темы (PUT).
    /// </summary>
    /// <param name="id">Идентификатор статуса темы.</param>
    /// <param name="name">Новое системное имя.</param>
    /// <param name="displayName">Новое отображаемое имя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Обновлённый статус темы или <c>null</c>, если не найден.</returns>
    Task<TopicStatusDto?> UpdateAsync(Guid id, string name, string displayName, CancellationToken ct);

    /// <summary>
    /// Частично обновляет статус темы (PATCH). Поля со значением <c>null</c> не изменяются.
    /// </summary>
    /// <param name="id">Идентификатор статуса темы.</param>
    /// <param name="name">Новое системное имя или <c>null</c> для сохранения текущего.</param>
    /// <param name="displayName">Новое отображаемое имя или <c>null</c> для сохранения текущего.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Обновлённый статус темы или <c>null</c>, если не найден.</returns>
    Task<TopicStatusDto?> PatchAsync(Guid id, string? name, string? displayName, CancellationToken ct);

    /// <summary>
    /// Удаляет статус темы по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор статуса темы.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если статус был удалён; <c>false</c>, если не найден.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Возвращает идентификатор статуса темы по его системному имени.
    /// </summary>
    Task<Guid?> GetIdByCodeNameAsync(string codeName, CancellationToken ct);
}
