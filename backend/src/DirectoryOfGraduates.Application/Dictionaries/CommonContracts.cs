namespace DirectoryOfGraduates.Application.Dictionaries;

/// <summary>
/// Базовый DTO для справочников с парой полей <c>Name</c>/<c>DisplayName</c>.
/// Подходит для большинства "простых" справочников проекта.
/// </summary>
/// <param name="Id">Уникальный идентификатор записи.</param>
/// <param name="Name">Системное имя (например, <c>Pending</c>).</param>
/// <param name="DisplayName">Отображаемое имя (например, <c>Ожидает ответа</c>).</param>
/// <param name="CreatedAt">Дата и время создания записи (UTC).</param>
/// <param name="UpdatedAt">Дата и время последнего обновления (UTC), null если не обновлялась.</param>
public record NamedDictionaryItemDto(
    Guid Id,
    string Name,
    string DisplayName,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>
/// Запрос на получение списка записей справочника с пагинацией и поиском.
/// </summary>
/// <param name="Q">Строка поиска по <c>Name</c> и <c>DisplayName</c> (регистронезависимый ILIKE).</param>
/// <param name="Page">Номер страницы (начиная с 1).</param>
/// <param name="PageSize">Количество элементов на странице (1–200).</param>
public record ListNamedDictionaryItemsQuery(string? Q, int Page = 1, int PageSize = 50);

/// <summary>
/// Команда для создания, полного (PUT) или частичного (PATCH) обновления записи справочника.
/// Для POST/PUT оба поля обязательны. Для PATCH поля со значением <c>null</c> не изменяются.
/// </summary>
/// <param name="Name">Системное имя.</param>
/// <param name="DisplayName">Отображаемое имя.</param>
public record UpsertNamedDictionaryItemCommand(string? Name, string? DisplayName);

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
/// Универсальный результат операции со справочником/сущностью.
/// </summary>
/// <typeparam name="T">Тип возвращаемого значения при успехе.</typeparam>
/// <typeparam name="TError">Enum тип ошибок конкретного домена.</typeparam>
/// <param name="Value">Значение при успешной операции.</param>
/// <param name="Error">Тип ошибки при неуспешной операции.</param>
/// <param name="Message">Сообщение об ошибке.</param>
public sealed record Result<T, TError>(T? Value, TError? Error, string Message)
    where TError : struct, Enum
{
    /// <summary>
    /// Создаёт успешный результат.
    /// </summary>
    /// <param name="value">Значение результата.</param>
    public static Result<T, TError> Ok(T value) => new(value, null, string.Empty);

    /// <summary>
    /// Создаёт результат с ошибкой.
    /// </summary>
    /// <param name="error">Тип ошибки.</param>
    /// <param name="message">Описание ошибки.</param>
    public static Result<T, TError> Fail(TError error, string message) => new(default, error, message);
}

