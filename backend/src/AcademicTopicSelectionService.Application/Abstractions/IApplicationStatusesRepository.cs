using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.ApplicationStatuses;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Репозиторий для работы со статусами заявки в базе данных 
/// </summary>
public interface IApplicationStatusesRepository
{
    /// <summary>
    /// Получает постраничный список статусов заявки с возможностью поиска
    /// </summary>
    /// <param name="query">Параметры запроса (поиск, пагинация).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Постраничный результат со списком статусов заявки.</returns>
    Task<PagedResult<ApplicationStatusDto>> ListAsync(ListApplicationStatusQuery query,  CancellationToken ct);
    
    /// <summary>
    /// Получает статус заявки по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор статуса заявки.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Статус заявки или <c>null</c>, если не найдена.</returns>
    Task<ApplicationStatusDto?> GetAsync(Guid id, CancellationToken ct);
    
    /// <summary>
    /// Проверяет существование статус заявки с указанным именем.
    /// </summary>
    /// <param name="name">Системное имя статуса заявки для проверки.</param>
    /// <param name="excludeId">Идентификатор статуса заявки,
    /// которую нужно исключить из проверки (для обновления).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если статус заявки с таким именем существует.</returns>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct);

    /// <summary>
    /// Создаёт новый статус заявки.
    /// </summary>
    /// <param name="name">Системное имя статуса заявки.</param>
    /// <param name="displayName">Отображаемое имя статуса заявки.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Созданный статус заявки с присвоенным идентификатором.</returns>
    Task<ApplicationStatusDto> CreateAsync(string name, string displayName, CancellationToken ct);
    
    /// <summary>
    /// Полностью обновляет статус заявки (PUT).
    /// </summary>
    /// <param name="id">Идентификатор статуса заявки.</param>
    /// <param name="name">Новое системное имя.</param>
    /// <param name="displayName">Новое отображаемое имя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Обновлённый статус заявки или <c>null</c>, если не найден.</returns>
    Task<ApplicationStatusDto?> UpdateAsync(Guid id, string name, string displayName, CancellationToken ct);
    
    /// <summary>
    /// Частично обновляет статус заявки (PATCH). Поля со значением <c>null</c> не изменяются.
    /// </summary>
    /// <param name="id">Идентификатор статуса заявки.</param>
    /// <param name="name">Новое системное имя или <c>null</c> для сохранения текущего.</param>
    /// <param name="displayName">Новое отображаемое имя или <c>null</c> для сохранения текущего.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Обновлённый статус заявки или <c>null</c>, если не найден.</returns>
    Task<ApplicationStatusDto?> PatchAsync(Guid id, string? name, string? displayName, CancellationToken ct);
    
    /// <summary>
    /// Удаляет статус заявки по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор статуса заявки.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если статус заявки был удален; <c>false</c>, если не найден.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}