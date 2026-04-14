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

public sealed record ListSupervisorRequestsQuery(
    int Page = 1,
    int PageSize = 50);

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
