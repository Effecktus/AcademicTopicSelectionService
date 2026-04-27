namespace AcademicTopicSelectionService.Application.SupervisorRequests;

public sealed record ApplicationStatusRefDto(Guid Id, string CodeName, string DisplayName);

public sealed record SupervisorRequestDto(
    Guid Id,
    Guid StudentId,
    string StudentFirstName,
    string StudentLastName,
    Guid TeacherUserId,
    string TeacherFirstName,
    string TeacherLastName,
    ApplicationStatusRefDto Status,
    string? Comment,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record SupervisorRequestDetailDto(
    Guid Id,
    Guid StudentId,
    string StudentFirstName,
    string StudentLastName,
    string StudentGroupName,
    Guid TeacherUserId,
    string TeacherFirstName,
    string TeacherLastName,
    string TeacherEmail,
    ApplicationStatusRefDto Status,
    string? Comment,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// Список запросов на научное руководство (пагинация, сортировка).
/// Параметр <c>Sort</c>: <c>createdAtDesc</c> (по умолчанию), <c>createdAtAsc</c>,
/// <c>statusAsc</c>, <c>statusDesc</c>, <c>counterpartyAsc</c>, <c>counterpartyDesc</c>
/// (для преподавателя — по ФИО студента, для студента — по ФИО преподавателя).
/// Параметры <c>CreatedFromUtc</c> / <c>CreatedToUtc</c> — фильтр по дате создания (UTC, включительно).
/// </summary>
public sealed record ListSupervisorRequestsQuery(
    int Page = 1,
    int PageSize = 50,
    string? Sort = null,
    DateTimeOffset? CreatedFromUtc = null,
    DateTimeOffset? CreatedToUtc = null);

public sealed record CreateSupervisorRequestCommand(
    Guid TeacherUserId,
    string? Comment);

public sealed record RejectSupervisorRequestCommand(
    string Comment);

public enum SupervisorRequestsError
{
    Validation,
    NotFound,
    Forbidden,
    Conflict,
    InvalidTransition
}
