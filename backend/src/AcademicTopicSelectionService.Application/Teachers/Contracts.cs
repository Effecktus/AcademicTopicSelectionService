using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Teachers;

/// <summary>
/// Карточка преподавателя для списка и детального просмотра.
/// </summary>
public sealed record TeacherDto(
    Guid Id,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? MiddleName,
    int? MaxStudentsLimit,
    DictionaryItemRefDto AcademicDegree,
    DictionaryItemRefDto AcademicTitle,
    DictionaryItemRefDto Position,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// Список преподавателей: поиск по ФИО и email, пагинация.
/// </summary>
/// <param name="Query">Подстрока (ILIKE по email, имени, фамилии, отчеству).</param>
/// <param name="Page">Номер страницы (с 1).</param>
/// <param name="PageSize">Размер страницы (1–200).</param>
public sealed record ListTeachersQuery(string? Query, int Page = 1, int PageSize = 50);
