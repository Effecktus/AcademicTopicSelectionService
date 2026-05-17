namespace AcademicTopicSelectionService.Application.Admin;

/// <summary>
/// Сводная аналитика для панели администратора.
/// </summary>
public sealed record AdminAnalyticsDto(
    List<StatusCountDto> ApplicationsByStatus,
    List<YearCountDto> GwByYear,
    List<DepartmentCountDto> ApplicationsByDepartment);

/// <summary>Количество заявок по статусу.</summary>
public sealed record StatusCountDto(string StatusCode, string StatusDisplayName, long Count);

/// <summary>Количество ВКР по году.</summary>
public sealed record YearCountDto(int Year, long Count);

/// <summary>Количество заявок по кафедре научного руководителя.</summary>
public sealed record DepartmentCountDto(string DepartmentName, long Count);

/// <summary>Строка экспорта ВКР.</summary>
public sealed record GwExportRow(
    string Title,
    string StudentFullName,
    string TeacherFullName,
    int Year,
    int Grade,
    string CommissionMembers,
    bool HasThesis,
    bool HasPresentation,
    DateTime CreatedAt);

/// <summary>Строка экспорта заявки.</summary>
public sealed record ApplicationExportRow(
    string TopicTitle,
    string StudentFullName,
    string StudentGroup,
    string SupervisorFullName,
    string StatusDisplayName,
    DateTime CreatedAt);

/// <summary>Строка экспорта пользователя.</summary>
public sealed record UserExportRow(
    string Email,
    string LastName,
    string FirstName,
    string? MiddleName,
    string RoleDisplayName,
    string? DepartmentDisplayName,
    bool IsActive,
    DateTime CreatedAt);
