namespace AcademicTopicSelectionService.Application.Dictionaries.ApplicationStatuses;

/// <summary>
/// DTO статуса заявки для передачи данных между слоями приложения.
/// </summary>
/// <param name="Id">Уникальный идентификатор статуса заявки.</param>
/// <param name="Name">Системное имя статуса заявки.</param>
/// <param name="DisplayName">Отображаемое имя статуса заявки.</param>
/// <param name="CreatedAt">Дата и время создания записи (UTC).</param>
/// <param name="UpdatedAt">Дата и время последнего обновления (UTC), null если не обновлялась.</param>
public sealed record ApplicationStatusDto(
    Guid Id, 
    string Name, 
    string DisplayName, 
    DateTime CreatedAt, 
    DateTime? UpdatedAt)
    : NamedDictionaryItemDto(Id, Name, DisplayName, CreatedAt, UpdatedAt);

/// <summary>
/// Запрос на получение списка статусов заявки с пагинацией и поиском.
/// </summary>
/// <param name="Query">Строка поиска по <c>Name</c> и <c>DisplayName</c> (регистронезависимый ILIKE).</param>
/// <param name="Page">Номер страницы (начиная с 1).</param>
/// <param name="PageSize">Количество элементов на странице (1–200).</param>
public sealed record ListApplicationStatusQuery(
    string? Query,
    int Page = 1,
    int PageSize = 50) 
    : ListNamedDictionaryItemsQuery(Query, Page, PageSize);

/// <summary>
/// Команда для создания, полного (PUT) или частичного (PATCH) обновления роли.
/// Для POST/PUT оба поля обязательны. Для PATCH поля со значением <c>null</c> не изменяются.
/// </summary>
/// <param name="Name">Системное имя статуса заявки.</param>
/// <param name="DisplayName">Отображаемое имя статуса заявки.</param>
public sealed record UpsetApplicationStatusCommand(
    string? Name,
    string? DisplayName) 
    : UpsertNamedDictionaryItemCommand(Name, DisplayName);

/// <summary>
/// Типы ошибок при работе со статусами заявки
/// </summary>
public enum ApplicationStatusesError
{
    /// <summary>
    /// Ошибка валидации входных данных.
    /// </summary>
    Validation,
    
    /// <summary>
    /// Статус заявки не найден по указанному идентификатору.
    /// </summary>
    NotFound,
    
    /// <summary>
    /// Конфликт: статус заявки с таким именем уже существует.
    /// </summary>
    Conflict,
}