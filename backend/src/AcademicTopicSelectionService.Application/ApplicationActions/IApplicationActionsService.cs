using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.ApplicationActions;

/// <summary>
/// Сервис бизнес-логики для работы с действиями по заявкам.
/// Управляет историей согласований: создаёт новые действия при смене этапа
/// и обновляет их статус при принятии решения ответственным лицом.
/// </summary>
public interface IApplicationActionsService
{
    /// <summary>
    /// Получает постраничный список действий для указанной заявки, упорядоченных по дате создания.
    /// </summary>
    /// <param name="query">Параметры запроса (фильтр по заявке, пагинация).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Постраничный результат со списком действий.</returns>
    Task<PagedResult<ApplicationActionDto>> ListByApplicationAsync(ListApplicationActionsQuery query,
        CancellationToken ct);

    /// <summary>
    /// Получает действие по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор действия.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Действие или <c>null</c>, если не найдено.</returns>
    Task<ApplicationActionDto?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Создаёт новое действие по заявке. Статус устанавливается в <c>Pending</c> автоматически.
    /// </summary>
    /// <param name="command">Данные для создания действия.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат операции: созданное действие или ошибка.</returns>
    Task<Result<ApplicationActionDto, ApplicationActionsError>> CreateAsync(
        CreateApplicationActionCommand command, CancellationToken ct);

    /// <summary>
    /// Частично обновляет действие (PATCH): статус и/или комментарий.
    /// Поля со значением <c>null</c> не изменяются.
    /// </summary>
    /// <param name="id">Идентификатор действия.</param>
    /// <param name="command">Данные для обновления.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат операции: обновлённое действие или ошибка.</returns>
    Task<Result<ApplicationActionDto, ApplicationActionsError>> UpdateAsync(
        Guid id, UpdateApplicationActionCommand command, CancellationToken ct);

    /// <summary>
    /// Удаляет действие по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор действия.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если действие было удалено; <c>false</c>, если не найдено.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
