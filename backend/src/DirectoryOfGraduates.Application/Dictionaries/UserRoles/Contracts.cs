namespace DirectoryOfGraduates.Application.Dictionaries.UserRoles;

/// <summary>
/// DTO роли пользователя для передачи данных между слоями приложения.
/// </summary>
/// <param name="Id">Уникальный идентификатор роли.</param>
/// <param name="Name">Системное имя роли (например, <c>Student</c>).</param>
/// <param name="DisplayName">Отображаемое имя роли (например, <c>Студент</c>).</param>
/// <param name="CreatedAt">Дата и время создания записи (UTC).</param>
/// <param name="UpdatedAt">Дата и время последнего обновления (UTC), null если не обновлялась.</param>
public sealed record UserRoleDto(Guid Id, string Name, string DisplayName, DateTime CreatedAt, DateTime? UpdatedAt);

/// <summary>
/// Запрос на получение списка ролей с пагинацией и поиском.
/// </summary>
/// <param name="Q">Строка поиска по <c>Name</c> и <c>DisplayName</c> (регистронезависимый ILIKE).</param>
/// <param name="Page">Номер страницы (начиная с 1).</param>
/// <param name="PageSize">Количество элементов на странице (1–200).</param>
public sealed record ListUserRolesQuery(string? Q, int Page = 1, int PageSize = 50);

/// <summary>
/// Команда для создания или полного обновления (PUT) роли.
/// Оба поля обязательны.
/// </summary>
/// <param name="Name">Системное имя роли.</param>
/// <param name="DisplayName">Отображаемое имя роли.</param>
public sealed record UpsertUserRoleCommand(string? Name, string? DisplayName);

/// <summary>
/// Команда для частичного обновления (PATCH) роли.
/// Поля, которые равны <c>null</c>, не будут изменены.
/// </summary>
/// <param name="Name">Системное имя роли (опционально).</param>
/// <param name="DisplayName">Отображаемое имя роли (опционально).</param>
public sealed record PatchUserRoleCommand(string? Name, string? DisplayName);

/// <summary>
/// Результат постраничного запроса.
/// </summary>
/// <typeparam name="T">Тип элементов списка.</typeparam>
/// <param name="Page">Текущая страница.</param>
/// <param name="PageSize">Размер страницы.</param>
/// <param name="Total">Общее количество элементов.</param>
/// <param name="Items">Элементы текущей страницы.</param>
public sealed record PagedResult<T>(int Page, int PageSize, long Total, IReadOnlyList<T> Items);

/// <summary>
/// Типы ошибок при работе с ролями пользователей.
/// </summary>
public enum UserRolesError
{
    /// <summary>Ошибка валидации входных данных.</summary>
    Validation,
    
    /// <summary>Роль не найдена по указанному идентификатору.</summary>
    NotFound,
    
    /// <summary>Конфликт: роль с таким именем уже существует.</summary>
    Conflict
}

/// <summary>
/// Результат операции с ролью пользователя.
/// </summary>
/// <typeparam name="T">Тип возвращаемого значения при успехе.</typeparam>
/// <param name="Value">Значение при успешной операции.</param>
/// <param name="Error">Тип ошибки при неуспешной операции.</param>
/// <param name="Message">Сообщение об ошибке.</param>
public sealed record Result<T>(T? Value, UserRolesError? Error, string Message)
{
    /// <summary>
    /// Создаёт успешный результат.
    /// </summary>
    /// <param name="value">Значение результата.</param>
    public static Result<T> Ok(T value) => new(value, null, string.Empty);
    
    /// <summary>
    /// Создаёт результат с ошибкой.
    /// </summary>
    /// <param name="error">Тип ошибки.</param>
    /// <param name="message">Описание ошибки.</param>
    public static Result<T> Fail(UserRolesError error, string message) => new(default, error, message);
}

