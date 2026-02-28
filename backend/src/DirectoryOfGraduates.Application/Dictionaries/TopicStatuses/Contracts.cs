namespace DirectoryOfGraduates.Application.Dictionaries.TopicStatuses;

/// <summary>
/// DTO статуса темы ВКР для передачи данных между слоями приложения.
/// </summary>
/// <param name="Id">Уникальный идентификатор статуса темы.</param>
/// <param name="Name">Системное имя статуса темы.</param>
/// <param name="DisplayName">Отображаемое имя статуса темы.</param>
/// <param name="CreatedAt">Дата и время создания записи (UTC).</param>
/// <param name="UpdatedAt">Дата и время последнего обновления (UTC), null если не обновлялась.</param>
public sealed record TopicStatusDto(
    Guid Id,
    string Name,
    string DisplayName,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
    : NamedDictionaryItemDto(Id, Name, DisplayName, CreatedAt, UpdatedAt);

/// <summary>
/// Запрос на получение списка статусов темы с пагинацией и поиском.
/// </summary>
/// <param name="Query">Строка поиска по <c>Name</c> и <c>DisplayName</c> (регистронезависимый ILIKE).</param>
/// <param name="Page">Номер страницы (начиная с 1).</param>
/// <param name="PageSize">Количество элементов на странице (1–200).</param>
public sealed record ListTopicStatusesQuery(
    string? Query,
    int Page = 1,
    int PageSize = 50)
    : ListNamedDictionaryItemsQuery(Query, Page, PageSize);

/// <summary>
/// Команда для создания, полного (PUT) или частичного (PATCH) обновления статуса темы.
/// Для POST/PUT оба поля обязательны. Для PATCH поля со значением <c>null</c> не изменяются.
/// </summary>
/// <param name="Name">Системное имя статуса темы.</param>
/// <param name="DisplayName">Отображаемое имя статуса темы.</param>
public sealed record UpsertTopicStatusCommand(
    string? Name,
    string? DisplayName)
    : UpsertNamedDictionaryItemCommand(Name, DisplayName);

/// <summary>
/// Типы ошибок при работе со статусами темы ВКР.
/// </summary>
public enum TopicStatusesError
{
    /// <summary>
    /// Ошибка валидации входных данных.
    /// </summary>
    Validation,

    /// <summary>
    /// Статус темы не найден по указанному идентификатору.
    /// </summary>
    NotFound,

    /// <summary>
    /// Конфликт: статус темы с таким именем уже существует.
    /// </summary>
    Conflict,
}
