namespace AcademicTopicSelectionService.Application.Dictionaries.UserRoles;

/// <summary>
/// DTO роли пользователя для передачи данных между слоями приложения.
/// </summary>
/// <param name="Id">Уникальный идентификатор роли.</param>
/// <param name="CodeName">Системное имя роли (например, <c>Student</c>).</param>
/// <param name="DisplayName">Отображаемое имя роли.</param>
/// <param name="CreatedAt">Дата и время создания записи (UTC).</param>
/// <param name="UpdatedAt">Дата и время последнего обновления (UTC), null если не обновлялась.</param>
public sealed record UserRoleDto(
    Guid Id, 
    string CodeName, 
    string DisplayName, 
    DateTime CreatedAt, 
    DateTime? UpdatedAt) 
    : NamedDictionaryItemDto(Id, CodeName, DisplayName, CreatedAt, UpdatedAt);

/// <summary>
/// Запрос на получение списка ролей с пагинацией и поиском.
/// </summary>
/// <param name="Query">Строка поиска по <c>CodeName</c> и <c>DisplayName</c> (регистронезависимый ILIKE).</param>
/// <param name="Page">Номер страницы (начиная с 1).</param>
/// <param name="PageSize">Количество элементов на странице (1–200).</param>
public sealed record ListUserRolesQuery(
    string? Query, 
    int Page = 1, 
    int PageSize = 50)
    : ListNamedDictionaryItemsQuery(Query, Page, PageSize);

/// <summary>
/// Команда для создания, полного (PUT) или частичного (PATCH) обновления роли.
/// Для POST/PUT оба поля обязательны. Для PATCH поля со значением <c>null</c> не изменяются.
/// </summary>
/// <param name="CodeName">Системное имя роли.</param>
/// <param name="DisplayName">Отображаемое имя роли.</param>
public sealed record UpsertUserRoleCommand(
    string? CodeName, 
    string? DisplayName)
    : UpsertNamedDictionaryItemCommand(CodeName, DisplayName);

/// <summary>
/// Типы ошибок при работе с ролями пользователей.
/// </summary>
public enum UserRolesError
{
    /// <summary>
    /// Ошибка валидации входных данных.
    /// </summary>
    Validation,
    
    /// <summary>
    /// Роль не найдена по указанному идентификатору.
    /// </summary>
    NotFound,
    
    /// <summary>
    /// Конфликт: роль с таким именем уже существует.
    /// </summary>
    Conflict
}
