namespace AcademicTopicSelectionService.Application.StudentApplications;

/// <summary>
/// Данные для проверки доступа к чату по заявке: участники, статус заявки и статус связанного запроса на научрука.
/// </summary>
public sealed record ApplicationChatAccessInfo(
    Guid StudentUserId,
    Guid TeacherUserId,
    string? SupervisorRequestStatusCode,
    bool HasSupervisorRequest,
    string ApplicationStatusCode);
