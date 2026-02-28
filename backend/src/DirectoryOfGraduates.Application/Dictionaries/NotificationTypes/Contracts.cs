namespace DirectoryOfGraduates.Application.Dictionaries.NotificationTypes;

/// <summary>
/// DTO типа уведомления для передачи данных между слоями приложения.
/// </summary>
/// <param name="Id">Уникальный идентификатор типа уведомления.</param>
/// <param name="Name">Системное имя типа уведомления.</param>
/// <param name="DisplayName">Отображаемое имя типа уведомления.</param>
/// <param name="CreatedAt">Дата и время создания записи (UTC).</param>
/// <param name="UpdatedAt">Дата и время последнего обновления (UTC), null если не обновлялась.</param>
public sealed record NotificationTypeDto(
    Guid Id,
    string Name,
    string DisplayName,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
    : NamedDictionaryItemDto(Id, Name, DisplayName, CreatedAt, UpdatedAt);

/// <summary>
/// Запрос на получение списка типов уведомлений с пагинацией и поиском.
/// </summary>
/// <param name="Query">Строка поиска по <c>Name</c> и <c>DisplayName</c> (регистронезависимый ILIKE).</param>
/// <param name="Page">Номер страницы (начиная с 1).</param>
/// <param name="PageSize">Количество элементов на странице (1–200).</param>
public sealed record ListNotificationTypesQuery(
    string? Query,
    int Page = 1,
    int PageSize = 50)
    : ListNamedDictionaryItemsQuery(Query, Page, PageSize);

/// <summary>
/// Команда для создания, полного (PUT) или частичного (PATCH) обновления типа уведомления.
/// Для POST/PUT оба поля обязательны. Для PATCH поля со значением <c>null</c> не изменяются.
/// </summary>
/// <param name="Name">Системное имя типа уведомления.</param>
/// <param name="DisplayName">Отображаемое имя типа уведомления.</param>
public sealed record UpsertNotificationTypeCommand(
    string? Name,
    string? DisplayName)
    : UpsertNamedDictionaryItemCommand(Name, DisplayName);

/// <summary>
/// Типы ошибок при работе с типами уведомлений.
/// </summary>
public enum NotificationTypesError
{
    /// <summary>
    /// Ошибка валидации входных данных.
    /// </summary>
    Validation,

    /// <summary>
    /// Тип уведомления не найден по указанному идентификатору.
    /// </summary>
    NotFound,

    /// <summary>
    /// Конфликт: тип уведомления с таким именем уже существует.
    /// </summary>
    Conflict,
}
