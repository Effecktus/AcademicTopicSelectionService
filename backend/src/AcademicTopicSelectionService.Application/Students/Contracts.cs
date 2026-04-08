namespace AcademicTopicSelectionService.Application.Students;

/// <summary>
/// Учебная группа студента (кратко).
/// </summary>
public sealed record StudyGroupRefDto(Guid Id, int CodeName);

/// <summary>
/// Карточка студента для списка и детального просмотра.
/// </summary>
public sealed record StudentDto(
    Guid Id,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? MiddleName,
    StudyGroupRefDto StudyGroup,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// Список студентов: поиск и фильтр по группе.
/// </summary>
/// <param name="Query">Подстрока (ILIKE по email, имени, фамилии, отчеству).</param>
/// <param name="GroupId">Только студенты указанной группы.</param>
/// <param name="Page">Номер страницы (с 1).</param>
/// <param name="PageSize">Размер страницы (1–200).</param>
public sealed record ListStudentsQuery(string? Query, Guid? GroupId, int Page = 1, int PageSize = 50);
