namespace AcademicTopicSelectionService.Application.Dictionaries.ApplicationStatuses;

/// <summary>
/// Сервис бизнес-логики для работы со статусами заявок.
/// Выполняет валидацию, проверку на уникальность и делегирует операции репозиторию.
/// </summary>
public interface IApplicationStatusesService
{
    /// <summary>
    /// Получает постраничный список статусов заявки с нормализацией параметров запроса.
    /// </summary>
    /// <param name="query">Параметры запроса (поиск, пагинация).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Постраничный результат со списком ролей.</returns>
    Task<PagedResult<ApplicationStatusDto>> ListAsync(ListApplicationStatusQuery query, CancellationToken ct);

    /// <summary>
    /// Получает статус заявки по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор статуса заявки.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Статус заявки или <c>null</c>, если не найден.</returns>
    Task<ApplicationStatusDto?> GetAsync(Guid id, CancellationToken ct);
    
    /// <summary>
    /// Создаёт новый статус заявки с валидацией и проверкой уникальности имени.
    /// </summary>
    /// <param name="command">Данные для создания статуса заявки.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат операции: созданный статус заявки или ошибка.</returns>
    Task<Result<ApplicationStatusDto, ApplicationStatusesError>> CreateAsync(UpsetApplicationStatusCommand command, 
        CancellationToken ct);
    
    /// <summary>
    /// Полностью обновляет статус заявки (PUT) с валидацией и проверкой уникальности имени.
    /// </summary>
    /// <param name="id">Идентификатор статуса заявки.</param>
    /// <param name="command">Новые данные статуса заявки.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат операции: обновлённый статус заявки или ошибка.</returns>
    Task<Result<ApplicationStatusDto, ApplicationStatusesError>> UpdateAsync(Guid id,
        UpsetApplicationStatusCommand command, CancellationToken ct);
    
    /// <summary>
    /// Частично обновляет статус заявки (PATCH). Поля со значением <c>null</c> не изменяются.
    /// </summary>
    /// <param name="id">Идентификатор статуса заявки.</param>
    /// <param name="command">Данные для частичного обновления.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат операции: обновлённый статус заявки или ошибка.</returns>
    Task<Result<ApplicationStatusDto, ApplicationStatusesError>> PatchAsync(Guid id,
        UpsetApplicationStatusCommand command, CancellationToken ct);
    
    /// <summary>
    /// Удаляет статус заявки по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор статуса заявки.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если статус заявки был удален; <c>false</c>, если не найден.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}