using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.TopicCreatorTypes;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Репозиторий для работы с типами создателей тем ВКР в базе данных.
/// </summary>
public interface ITopicCreatorTypesRepository
{
    /// <summary>
    /// Получает постраничный список типов создателей тем с возможностью поиска.
    /// </summary>
    /// <param name="query">Параметры запроса (поиск, пагинация).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Постраничный результат со списком типов создателей тем.</returns>
    Task<PagedResult<TopicCreatorTypeDto>> ListAsync(ListTopicCreatorTypesQuery query, CancellationToken ct);

    /// <summary>
    /// Получает тип создателя темы по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор типа.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Тип создателя темы или <c>null</c>, если не найден.</returns>
    Task<TopicCreatorTypeDto?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Проверяет существование типа с указанным именем.
    /// </summary>
    /// <param name="name">Системное имя для проверки.</param>
    /// <param name="excludeId">Идентификатор записи, которую нужно исключить из проверки (для обновления).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если тип с таким именем существует.</returns>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct);

    /// <summary>
    /// Создаёт новый тип создателя темы.
    /// </summary>
    /// <param name="name">Системное имя.</param>
    /// <param name="displayName">Отображаемое имя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Созданный тип с присвоенным идентификатором.</returns>
    Task<TopicCreatorTypeDto> CreateAsync(string name, string displayName, CancellationToken ct);

    /// <summary>
    /// Полностью обновляет тип создателя темы (PUT).
    /// </summary>
    /// <param name="id">Идентификатор типа.</param>
    /// <param name="name">Новое системное имя.</param>
    /// <param name="displayName">Новое отображаемое имя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Обновлённый тип или <c>null</c>, если не найден.</returns>
    Task<TopicCreatorTypeDto?> UpdateAsync(Guid id, string name, string displayName, CancellationToken ct);

    /// <summary>
    /// Частично обновляет тип создателя темы (PATCH). Поля со значением <c>null</c> не изменяются.
    /// </summary>
    /// <param name="id">Идентификатор типа.</param>
    /// <param name="name">Новое системное имя или <c>null</c> для сохранения текущего.</param>
    /// <param name="displayName">Новое отображаемое имя или <c>null</c> для сохранения текущего.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Обновлённый тип или <c>null</c>, если не найден.</returns>
    Task<TopicCreatorTypeDto?> PatchAsync(Guid id, string? name, string? displayName, CancellationToken ct);

    /// <summary>
    /// Удаляет тип создателя темы по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор типа.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если запись была удалена; <c>false</c>, если не найдена.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
