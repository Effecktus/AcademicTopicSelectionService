namespace AcademicTopicSelectionService.Application.Admin;

/// <summary>
/// Сводная аналитика для панели администратора.
/// </summary>
public sealed record AdminAnalyticsDto(
    AdminSummaryDto Summary,
    List<StatusCountDto> ApplicationsByStatus,
    List<YearCountDto> GwByYear,
    List<DepartmentCountDto> ApplicationsByDepartment,
    List<MonthCountDto> ApplicationsByMonth,
    List<TeacherCountDto> TopTeachersByApplications);

/// <summary>Сводные счётчики.</summary>
public sealed record AdminSummaryDto(
    long TotalApplications,
    long TotalGraduateWorks,
    long TotalUsers);

/// <summary>Количество заявок по статусу.</summary>
public sealed record StatusCountDto(string StatusCode, string StatusDisplayName, long Count);

/// <summary>Количество ВКР по году.</summary>
public sealed record YearCountDto(int Year, long Count);

/// <summary>Количество заявок по кафедре научного руководителя.</summary>
public sealed record DepartmentCountDto(string DepartmentName, long Count);

/// <summary>Количество заявок по месяцу текущего года.</summary>
public sealed record MonthCountDto(int Month, long Count);

/// <summary>Количество заявок у научного руководителя (топ-5).</summary>
public sealed record TeacherCountDto(string TeacherFullName, string? DepartmentName, long Count);

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
