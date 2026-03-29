namespace AcademicTopicSelectionService.Application.Dictionaries.ApplicationActionStatuses;

/// <summary>
/// DTO статуса действия по заявке для передачи данных между слоями приложения.
/// </summary>
/// <param name="Id">Уникальный идентификатор статуса действия.</param>
/// <param name="CodeName">Системное имя статуса действия.</param>
/// <param name="DisplayName">Отображаемое имя статуса действия.</param>
/// <param name="CreatedAt">Дата и время создания записи (UTC).</param>
/// <param name="UpdatedAt">Дата и время последнего обновления (UTC), null если не обновлялась.</param>
public sealed record ApplicationActionStatusDto(
    Guid Id,
    string CodeName,
    string DisplayName,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
    : NamedDictionaryItemDto(Id, CodeName, DisplayName, CreatedAt, UpdatedAt);

/// <summary>
/// Запрос на получение списка статусов действий с пагинацией и поиском.
/// </summary>
/// <param name="Query">Строка поиска по <c>CodeName</c> и <c>DisplayName</c> (регистронезависимый ILIKE).</param>
/// <param name="Page">Номер страницы (начиная с 1).</param>
/// <param name="PageSize">Количество элементов на странице (1–200).</param>
public sealed record ListApplicationActionStatusQuery(
    string? Query,
    int Page = 1,
    int PageSize = 50)
    : ListNamedDictionaryItemsQuery(Query, Page, PageSize);

/// <summary>
/// Команда для создания, полного (PUT) или частичного (PATCH) обновления статуса действия.
/// Для POST/PUT оба поля обязательны. Для PATCH поля со значением <c>null</c> не изменяются.
/// </summary>
/// <param name="CodeName">Системное имя статуса действия.</param>
/// <param name="DisplayName">Отображаемое имя статуса действия.</param>
public sealed record UpsertApplicationActionStatusCommand(
    string? CodeName,
    string? DisplayName)
    : UpsertNamedDictionaryItemCommand(CodeName, DisplayName);

/// <summary>
/// Типы ошибок при работе со статусами действий по заявкам
/// </summary>
public enum ApplicationActionStatusesError
{
    /// <summary>
    /// Ошибка валидации входных данных.
    /// </summary>
    Validation,

    /// <summary>
    /// Статус действия не найден по указанному идентификатору.
    /// </summary>
    NotFound,

    /// <summary>
    /// Конфликт: статус действия с таким именем уже существует.
    /// </summary>
    Conflict,
}
