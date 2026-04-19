using AcademicTopicSelectionService.Application.ApplicationActions;
using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Репозиторий для работы с действиями по заявкам в базе данных.
/// </summary>
public interface IApplicationActionsRepository
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
    /// Проверяет, существует ли заявка с указанным идентификатором.
    /// </summary>
    /// <param name="applicationId">Идентификатор заявки.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если заявка существует.</returns>
    Task<bool> ApplicationExistsAsync(Guid applicationId, CancellationToken ct);

    /// <summary>
    /// Чтение истории действий разрешено студенту-автору заявки, научруку из привязанного запроса
    /// или пользователю, который указан ответственным хотя бы в одном действии по этой заявке.
    /// </summary>
    Task<bool> UserCanReadApplicationActionsAsync(Guid applicationId, Guid userId, CancellationToken ct);

    /// <summary>
    /// Проверяет, существует ли пользователь с указанным идентификатором.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если пользователь существует.</returns>
    Task<bool> UserExistsAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Получает идентификатор статуса действия по системному имени.
    /// </summary>
    /// <param name="codeName">Системное имя статуса (например, <c>Pending</c>).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Идентификатор статуса или <c>null</c>, если не найден.</returns>
    Task<Guid?> GetActionStatusIdByCodeNameAsync(string codeName, CancellationToken ct);

    /// <summary>
    /// Проверяет, существует ли статус действия с указанным идентификатором.
    /// </summary>
    /// <param name="statusId">Идентификатор статуса.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если статус существует.</returns>
    Task<bool> ActionStatusExistsAsync(Guid statusId, CancellationToken ct);

    /// <summary>
    /// Создаёт новое действие по заявке.
    /// </summary>
    /// <param name="applicationId">Идентификатор заявки.</param>
    /// <param name="ResponsibleId">Идентификатор ответственного пользователя.</param>
    /// <param name="statusId">Идентификатор статуса действия.</param>
    /// <param name="comment">Комментарий (необязательно).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Созданное действие с присвоенным идентификатором.</returns>
    Task<ApplicationActionDto> CreateAsync(Guid applicationId, Guid ResponsibleId, Guid statusId,
        string? comment, CancellationToken ct);

    /// <summary>
    /// Добавляет действие в DbContext без вызова SaveChanges.
    /// Используется для атомарной записи: сначала добавляем action, потом вызываем
    /// единый SaveChangesAsync из репозитория заявок, чтобы статус и action сохранились вместе.
    /// </summary>
    void Enqueue(Guid applicationId, Guid responsibleId, Guid statusId, string? comment);

    /// <summary>
    /// Обновляет статус и/или комментарий действия. Параметры со значением <c>null</c> не изменяются.
    /// </summary>
    /// <param name="id">Идентификатор действия.</param>
    /// <param name="statusId">Новый идентификатор статуса или <c>null</c>.</param>
    /// <param name="comment">Новый комментарий или <c>null</c>.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Обновлённое действие или <c>null</c>, если не найдено.</returns>
    Task<ApplicationActionDto?> UpdateAsync(Guid id, Guid? statusId, string? comment, CancellationToken ct);

    /// <summary>
    /// Удаляет действие по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор действия.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если действие было удалено; <c>false</c>, если не найдено.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
