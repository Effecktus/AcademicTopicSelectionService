namespace AcademicTopicSelectionService.Application.DepartmentHead;

/// <summary>
/// Аналитика для заведующего кафедрой (данные по его кафедре).
/// </summary>
public sealed record DepartmentHeadAnalyticsDto(
    DhSummaryDto Summary,
    List<DhStatusCountDto> ApplicationsByStatus,
    List<DhYearCountDto> GwByYear,
    List<DhTeacherWorkloadDto> TeacherWorkload);

/// <summary>Сводные счётчики по кафедре.</summary>
public sealed record DhSummaryDto(
    long TotalTopics,
    long TotalStudents,
    long TotalApplications,
    long TotalGraduateWorks);

/// <summary>Количество заявок по статусу (кафедра).</summary>
public sealed record DhStatusCountDto(string StatusCode, string StatusDisplayName, long Count);

/// <summary>Количество ВКР по году (кафедра).</summary>
public sealed record DhYearCountDto(int Year, long Count);

/// <summary>Нагрузка преподавателя кафедры: сколько активных студентов.</summary>
public sealed record DhTeacherWorkloadDto(
    string TeacherFullName,
    int ActiveStudentsCount,
    int? MaxStudentsLimit);
