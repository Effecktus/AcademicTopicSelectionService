namespace AcademicTopicSelectionService.Application.GraduateWorks;

/// <summary>
/// Запрос списка записей архива ВКР.
/// </summary>
/// <param name="Sort">
/// Сортировка: <c>yearDesc</c> (по умолчанию), <c>yearAsc</c>,
/// <c>titleAsc</c>, <c>titleDesc</c>,
/// <c>studentAsc</c>, <c>studentDesc</c>,
/// <c>teacherAsc</c>, <c>teacherDesc</c>,
/// <c>gradeAsc</c>, <c>gradeDesc</c>.
/// </param>
public sealed record ListGraduateWorksQuery(
    int Page = 1,
    int PageSize = 50,
    int? Year = null,
    string? TitleQuery = null,
    Guid? TeacherId = null,
    string? TeacherQuery = null,
    string? Sort = null);

/// <summary>
/// Создание записи ВКР администратором (студент и научрук берутся из заявки).
/// </summary>
public sealed record CreateGraduateWorkCommand(
    Guid ApplicationId,
    string Title,
    int Year,
    int Grade,
    string CommissionMembers);

/// <summary>
/// Полное обновление метаданных ВКР.
/// </summary>
public sealed record UpdateGraduateWorkCommand(
    Guid Id,
    string Title,
    int Year,
    int Grade,
    string CommissionMembers);

/// <summary>
/// DTO записи ВКР в архиве.
/// </summary>
public sealed record GraduateWorkDto(
    Guid Id,
    Guid ApplicationId,
    Guid StudentId,
    Guid TeacherId,
    string Title,
    int Year,
    int Grade,
    string CommissionMembers,
    bool HasFile,
    bool HasPresentation,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? FileName = null,
    string? PresentationFileName = null,
    string StudentFullName = "",
    string TeacherFullName = "");
