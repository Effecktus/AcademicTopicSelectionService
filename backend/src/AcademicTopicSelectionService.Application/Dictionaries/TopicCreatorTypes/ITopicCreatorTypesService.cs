namespace AcademicTopicSelectionService.Application.Dictionaries.TopicCreatorTypes;

/// <summary>
/// Сервис бизнес-логики для работы с типами создателей тем ВКР.
/// Выполняет валидацию, проверку на уникальность и делегирует операции репозиторию.
/// </summary>
public interface ITopicCreatorTypesService
{
    /// <summary>
    /// Получает постраничный список типов создателей тем с нормализацией параметров запроса.
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
    /// Создаёт новый тип создателя темы с валидацией и проверкой уникальности имени.
    /// </summary>
    /// <param name="command">Данные для создания типа.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат операции: созданный тип или ошибка.</returns>
    Task<Result<TopicCreatorTypeDto, TopicCreatorTypesError>> CreateAsync(UpsertTopicCreatorTypeCommand command, CancellationToken ct);

    /// <summary>
    /// Полностью обновляет тип создателя темы (PUT) с валидацией и проверкой уникальности имени.
    /// </summary>
    /// <param name="id">Идентификатор типа.</param>
    /// <param name="command">Новые данные типа.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат операции: обновлённый тип или ошибка.</returns>
    Task<Result<TopicCreatorTypeDto, TopicCreatorTypesError>> UpdateAsync(Guid id, UpsertTopicCreatorTypeCommand command, CancellationToken ct);

    /// <summary>
    /// Частично обновляет тип создателя темы (PATCH). Поля со значением <c>null</c> не изменяются.
    /// </summary>
    /// <param name="id">Идентификатор типа.</param>
    /// <param name="command">Данные для частичного обновления.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат операции: обновлённый тип или ошибка.</returns>
    Task<Result<TopicCreatorTypeDto, TopicCreatorTypesError>> PatchAsync(Guid id, UpsertTopicCreatorTypeCommand command, CancellationToken ct);

    /// <summary>
    /// Удаляет тип создателя темы по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор типа.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если тип был удалён; <c>false</c>, если не найден.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
