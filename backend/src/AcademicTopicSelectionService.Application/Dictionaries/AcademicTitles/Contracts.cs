using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Dictionaries.AcademicTitles;

/// <summary>
/// DTO учёного звания для передачи данных между слоями приложения.
/// </summary>
/// <param name="Id">Уникальный идентификатор учёного звания.</param>
/// <param name="Name">Системное имя звания.</param>
/// <param name="DisplayName">Отображаемое имя звания.</param>
/// <param name="CreatedAt">Дата и время создания записи (UTC).</param>
/// <param name="UpdatedAt">Дата и время последнего обновления (UTC), null если не обновлялась.</param>
public sealed record AcademicTitleDto(
    Guid Id,
    string Name,
    string DisplayName,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
    : NamedDictionaryItemDto(Id, Name, DisplayName, CreatedAt, UpdatedAt);

/// <summary>
/// Запрос на получение списка учёных званий с пагинацией и поиском.
/// </summary>
/// <param name="Query">Строка поиска по <c>Name</c> и <c>DisplayName</c> (регистронезависимый ILIKE).</param>
/// <param name="Page">Номер страницы (начиная с 1).</param>
/// <param name="PageSize">Количество элементов на странице (1–200).</param>
public sealed record ListAcademicTitlesQuery(
    string? Query,
    int Page = 1,
    int PageSize = 50)
    : ListNamedDictionaryItemsQuery(Query, Page, PageSize);

/// <summary>
/// Команда для создания, полного (PUT) или частичного (PATCH) обновления учёного звания.
/// Для POST/PUT оба поля обязательны. Для PATCH поля со значением <c>null</c> не изменяются.
/// </summary>
/// <param name="Name">Системное имя учёного звания.</param>
/// <param name="DisplayName">Отображаемое имя учёного звания.</param>
public sealed record UpsertAcademicTitleCommand(
    string? Name,
    string? DisplayName)
    : UpsertNamedDictionaryItemCommand(Name, DisplayName);

/// <summary>
/// Типы ошибок при работе с учёными званиями.
/// </summary>
public enum AcademicTitlesError
{
    /// <summary>
    /// Ошибка валидации входных данных.
    /// </summary>
    Validation,

    /// <summary>
    /// Учёное звание не найдено по указанному идентификатору.
    /// </summary>
    NotFound,

    /// <summary>
    /// Конфликт: учёное звание с таким именем уже существует.
    /// </summary>
    Conflict,
}
