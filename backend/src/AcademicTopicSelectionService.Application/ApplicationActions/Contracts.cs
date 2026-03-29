using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.ApplicationActions;

/// <summary>
/// DTO действия по заявке.
/// </summary>
/// <param name="Id">Уникальный идентификатор действия.</param>
/// <param name="ApplicationId">Идентификатор заявки.</param>
/// <param name="ResponsibleId">Идентификатор ответственного пользователя.</param>
/// <param name="StatusId">Идентификатор статуса действия.</param>
/// <param name="StatusCodeName">Системное имя статуса (например, <c>Pending</c>).</param>
/// <param name="StatusDisplayName">Отображаемое имя статуса (например, <c>На согласовании</c>).</param>
/// <param name="Comment">Комментарий ответственного (причина отклонения или пояснение).</param>
/// <param name="CreatedAt">Дата и время создания действия (UTC).</param>
/// <param name="UpdatedAt">Дата и время последнего обновления (UTC), null если не обновлялось.</param>
public sealed record ApplicationActionDto(
    Guid Id,
    Guid ApplicationId,
    Guid ResponsibleId,
    Guid StatusId,
    string StatusCodeName,
    string StatusDisplayName,
    string? Comment,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// Команда для создания нового действия по заявке.
/// Статус устанавливается в <c>Pending</c> автоматически.
/// </summary>
/// <param name="ApplicationId">Идентификатор заявки.</param>
/// <param name="ResponsibleId">Идентификатор пользователя, ответственного за согласование.</param>
/// <param name="Comment">Необязательный комментарий при создании действия.</param>
public sealed record CreateApplicationActionCommand(
    Guid ApplicationId,
    Guid ResponsibleId,
    string? Comment);

/// <summary>
/// Команда для обновления статуса и/или комментария действия (PATCH).
/// Поле со значением <c>null</c> не изменяется.
/// </summary>
/// <param name="StatusId">Новый идентификатор статуса действия.</param>
/// <param name="Comment">Новый комментарий или <c>null</c> для сохранения текущего.</param>
public sealed record UpdateApplicationActionCommand(
    Guid? StatusId,
    string? Comment);

/// <summary>
/// Запрос на получение списка действий по заявке.
/// </summary>
/// <param name="ApplicationId">Фильтр по идентификатору заявки (обязателен).</param>
/// <param name="Page">Номер страницы (начиная с 1).</param>
/// <param name="PageSize">Количество элементов на странице (1–200).</param>
public sealed record ListApplicationActionsQuery(
    Guid ApplicationId,
    int Page = 1,
    int PageSize = 50);

/// <summary>
/// Типы ошибок при работе с действиями по заявкам.
/// </summary>
public enum ApplicationActionsError
{
    /// <summary>
    /// Ошибка валидации входных данных.
    /// </summary>
    Validation,

    /// <summary>
    /// Действие не найдено по указанному идентификатору.
    /// </summary>
    NotFound,

    /// <summary>
    /// Указанная заявка не существует.
    /// </summary>
    ApplicationNotFound,

    /// <summary>
    /// Указанный ответственный пользователь не существует.
    /// </summary>
    ResponsibleUserNotFound,

    /// <summary>
    /// Указанный статус действия не существует.
    /// </summary>
    StatusNotFound,
}
