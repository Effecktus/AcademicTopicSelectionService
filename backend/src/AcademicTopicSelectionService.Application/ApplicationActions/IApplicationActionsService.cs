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
    Task<Result<PagedResult<ApplicationActionDto>, ApplicationActionsError>> ListByApplicationAsync(
        ListApplicationActionsQuery query,
        ApplicationActionsActor actor,
        CancellationToken ct);

    /// <summary>
    /// Получает действие по идентификатору.
    /// </summary>
    Task<Result<ApplicationActionDto, ApplicationActionsError>> GetAsync(Guid id, ApplicationActionsActor actor,
        CancellationToken ct);

    /// <summary>
    /// Создаёт новое действие по заявке. Статус устанавливается в <c>Pending</c> автоматически.
    /// </summary>
    Task<Result<ApplicationActionDto, ApplicationActionsError>> CreateAsync(
        CreateApplicationActionCommand command,
        ApplicationActionsActor actor,
        CancellationToken ct);

    /// <summary>
    /// Частично обновляет действие (PATCH): статус и/или комментарий.
    /// Поля со значением <c>null</c> не изменяются.
    /// </summary>
    Task<Result<ApplicationActionDto, ApplicationActionsError>> UpdateAsync(
        Guid id,
        UpdateApplicationActionCommand command,
        ApplicationActionsActor actor,
        CancellationToken ct);

    /// <summary>
    /// Удаляет действие по идентификатору.
    /// </summary>
    /// <returns><c>true</c> в значении результата, если запись удалена.</returns>
    Task<Result<bool, ApplicationActionsError>> DeleteAsync(Guid id, ApplicationActionsActor actor,
        CancellationToken ct);
}
