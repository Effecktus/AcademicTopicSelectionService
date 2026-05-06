using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.StudentApplications;

/// <summary>
/// DTO заявки студента для списка и детального просмотра.
/// </summary>
public sealed record StudentApplicationDto(
    Guid Id,
    Guid StudentId,
    string StudentFirstName,
    string StudentLastName,
    string StudentGroupName,
    Guid TopicId,
    string TopicTitle,
    Guid SupervisorRequestId,
    Guid SupervisorUserId,
    string SupervisorFirstName,
    string SupervisorLastName,
    Guid TopicCreatedByUserId,
    string TopicCreatedByEmail,
    string TopicCreatedByFirstName,
    string TopicCreatedByLastName,
    ApplicationStatusRefDto Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    /// <summary>
    /// Собирает DTO списка из детального представления (в списке email создателя темы не используется).
    /// </summary>
    public static StudentApplicationDto FromDetail(StudentApplicationDetailDto detail, string topicCreatedByEmail = "")
        => new(
            detail.Id,
            detail.StudentId,
            detail.StudentFirstName,
            detail.StudentLastName,
            detail.StudentGroupName,
            detail.TopicId,
            detail.TopicTitle,
            detail.SupervisorRequestId ?? Guid.Empty,
            detail.SupervisorUserId,
            detail.SupervisorFirstName,
            detail.SupervisorLastName,
            detail.TopicCreatedByUserId,
            topicCreatedByEmail,
            detail.TopicCreatedByFirstName,
            detail.TopicCreatedByLastName,
            detail.Status,
            detail.CreatedAt,
            detail.UpdatedAt);
}

/// <summary>
/// Краткая ссылка на статус заявки.
/// </summary>
public sealed record ApplicationStatusRefDto(Guid Id, string CodeName, string DisplayName);

/// <summary>
/// Заявка с историей действий (детальный просмотр).
/// </summary>
public sealed record StudentApplicationDetailDto(
    Guid Id,
    Guid StudentId,
    string StudentFirstName,
    string StudentLastName,
    string StudentGroupName,
    Guid TopicId,
    string TopicTitle,
    string? TopicDescription,
    Guid? SupervisorRequestId,
    Guid SupervisorUserId,
    string SupervisorFirstName,
    string SupervisorLastName,
    Guid? SupervisorDepartmentId,
    Guid TopicCreatedByUserId,
    string TopicCreatedByFirstName,
    string TopicCreatedByLastName,
    Guid? TopicSupervisorDepartmentId,
    ApplicationStatusRefDto Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<ApplicationActionSnapshotDto> Actions,
    IReadOnlyList<ApplicationTopicChangeHistoryEntryDto> TopicChangeHistory);

/// <summary>
/// Краткая запись действия из истории заявки.
/// </summary>
public sealed record ApplicationActionSnapshotDto(
    Guid Id,
    Guid ResponsibleId,
    string ResponsibleFirstName,
    string ResponsibleLastName,
    string StatusCodeName,
    string StatusDisplayName,
    string? Comment,
    DateTime CreatedAt);

/// <summary>
/// Запись истории изменения названия или описания темы по заявке.
/// </summary>
public sealed record ApplicationTopicChangeHistoryEntryDto(
    Guid Id,
    Guid ChangedByUserId,
    string ChangedByFirstName,
    string ChangedByLastName,
    string ChangeKind,
    string ChangeKindDisplayName,
    string? NewValue,
    DateTime CreatedAt);

/// <summary>
/// Параметры списка заявок: фильтрация по роли.
/// </summary>
/// <param name="Page">Номер страницы (с 1).</param>
/// <param name="PageSize">Размер страницы (1–200).</param>
public sealed record ListApplicationsQuery(
    int Page = 1,
    int PageSize = 50);

/// <summary>
/// Команда на создание заявки студентом.
/// </summary>
/// <param name="TopicId">Идентификатор существующей темы (опционально, если не передан ProposedTitle).</param>
/// <param name="SupervisorRequestId">Идентификатор одобренного запроса на научрука.</param>
/// <param name="ProposedTitle">Предложенная студентом новая тема (опционально, если не передан TopicId).</param>
/// <param name="ProposedDescription">Описание предложенной темы.</param>
public sealed record CreateApplicationCommand(
    Guid? TopicId,
    Guid SupervisorRequestId = default,
    string? ProposedTitle = null,
    string? ProposedDescription = null);

/// <summary>
/// Команда: студент обновляет название и описание темы по заявке (только в статусе OnEditing).
/// </summary>
public sealed record UpdateApplicationTopicCommand(string Title, string? Description);

/// <summary>
/// Команда: преподаватель или заведующий возвращает заявку студенту на доработку.
/// </summary>
/// <param name="Comment">Комментарий (обязательно).</param>
public sealed record ReturnApplicationForEditingCommand(string Comment);

/// <summary>
/// Команда: преподаватель одобряет заявку.
/// </summary>
/// <param name="Comment">Комментарий (опционально).</param>
public sealed record ApproveBySupervisorCommand(string? Comment);

/// <summary>
/// Команда: преподаватель отклоняет заявку.
/// </summary>
/// <param name="Comment">Причина отклонения (обязательно).</param>
public sealed record RejectBySupervisorCommand(string Comment);

/// <summary>
/// Команда: преподаватель передаёт заявку заведующему кафедрой.
/// </summary>
/// <param name="Comment">Комментарий (опционально).</param>
public sealed record SubmitToDepartmentHeadCommand(string? Comment);

/// <summary>
/// Команда: заведующий кафедрой утверждает заявку.
/// </summary>
/// <param name="Comment">Комментарий (опционально).</param>
public sealed record ApproveByDepartmentHeadCommand(string? Comment);

/// <summary>
/// Команда: заведующий кафедрой отклоняет заявку.
/// </summary>
/// <param name="Comment">Причина отклонения (обязательно).</param>
public sealed record RejectByDepartmentHeadCommand(string Comment);

/// <summary>
/// Типы ошибок при работе с заявками.
/// </summary>
public enum ApplicationsError
{
    /// <summary>
    /// Ошибка валидации входных данных.
    /// </summary>
    Validation,

    /// <summary>
    /// Заявка не найдена.
    /// </summary>
    NotFound,

    /// <summary>
    /// Нет прав на выполнение операции.
    /// </summary>
    Forbidden,

    /// <summary>
    /// Конфликт: тема уже занята другим студентом.
    /// </summary>
    Conflict,

    /// <summary>
    /// Недопустимый переход статуса.
    /// </summary>
    InvalidTransition,

    /// <summary>
    /// Превышен лимит студентов у научрука.
    /// </summary>
    SupervisorLimitExceeded,
}
