namespace AcademicTopicSelectionService.Application.Dictionaries.ApplicationActionStatuses;

/// <summary>
/// Сервис бизнес-логики для работы со статусами действий по заявкам.
/// Выполняет валидацию, проверку на уникальность и делегирует операции репозиторию.
/// </summary>
public interface IApplicationActionStatusesService
{
    /// <summary>
    /// Получает постраничный список статусов действий с нормализацией параметров запроса.
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
    /// Создаёт новый статус действия с валидацией и проверкой уникальности имени.
    /// </summary>
    /// <param name="command">Данные для создания статуса действия.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат операции: созданный статус или ошибка.</returns>
    Task<Result<ApplicationActionStatusDto, ApplicationActionStatusesError>> CreateAsync(
        UpsertApplicationActionStatusCommand command, CancellationToken ct);

    /// <summary>
    /// Полностью обновляет статус действия (PUT) с валидацией и проверкой уникальности имени.
    /// </summary>
    /// <param name="id">Идентификатор статуса действия.</param>
    /// <param name="command">Новые данные статуса действия.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат операции: обновлённый статус или ошибка.</returns>
    Task<Result<ApplicationActionStatusDto, ApplicationActionStatusesError>> UpdateAsync(
        Guid id, UpsertApplicationActionStatusCommand command, CancellationToken ct);

    /// <summary>
    /// Частично обновляет статус действия (PATCH). Поля со значением <c>null</c> не изменяются.
    /// </summary>
    /// <param name="id">Идентификатор статуса действия.</param>
    /// <param name="command">Данные для частичного обновления.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат операции: обновлённый статус или ошибка.</returns>
    Task<Result<ApplicationActionStatusDto, ApplicationActionStatusesError>> PatchAsync(
        Guid id, UpsertApplicationActionStatusCommand command, CancellationToken ct);

    /// <summary>
    /// Удаляет статус действия по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор статуса действия.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если статус был удалён; <c>false</c>, если не найден.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
