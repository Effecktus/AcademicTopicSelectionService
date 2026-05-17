namespace AcademicTopicSelectionService.Application.Users;

/// <summary>
/// Запрос на получение списка пользователей с фильтрацией и пагинацией.
/// </summary>
public sealed record ListUsersQuery(
    int Page = 1,
    int PageSize = 50,
    Guid? RoleId = null,
    string? Query = null);

/// <summary>
/// Краткое представление пользователя для отображения в таблице администратора.
/// </summary>
public sealed record UserListItemDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? MiddleName,
    string RoleCodeName,
    string RoleDisplayName,
    Guid? DepartmentId,
    string? DepartmentDisplayName,
    bool IsActive,
    DateTime CreatedAt);
