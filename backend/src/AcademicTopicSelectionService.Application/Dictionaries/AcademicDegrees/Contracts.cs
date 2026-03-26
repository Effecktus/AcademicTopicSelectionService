using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Dictionaries.AcademicDegrees;

/// <summary>
/// DTO учёной степени для передачи данных между слоями приложения.
/// </summary>
/// <param name="Id">Уникальный идентификатор учёной степени.</param>
/// <param name="CodeName">Системное имя степени.</param>
/// <param name="DisplayName">Отображаемое имя степени.</param>
/// <param name="ShortName">Сокращённое название (опционально).</param>
/// <param name="CreatedAt">Дата и время создания записи (UTC).</param>
/// <param name="UpdatedAt">Дата и время последнего обновления (UTC), null если не обновлялась.</param>
public sealed record AcademicDegreeDto(
    Guid Id,
    string CodeName,
    string DisplayName,
    string? ShortName,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// Запрос на получение списка учёных степеней с пагинацией и поиском.
/// </summary>
/// <param name="Query">Строка поиска по <c>CodeName</c>, <c>DisplayName</c> и <c>ShortName</c> (регистронезависимый ILIKE).</param>
/// <param name="Page">Номер страницы (начиная с 1).</param>
/// <param name="PageSize">Количество элементов на странице (1–200).</param>
public sealed record ListAcademicDegreesQuery(
    string? Query,
    int Page = 1,
    int PageSize = 50)
    : ListNamedDictionaryItemsQuery(Query, Page, PageSize);

/// <summary>
/// Команда для создания, полного (PUT) или частичного (PATCH) обновления учёной степени.
/// Для POST/PUT CodeName и DisplayName обязательны, ShortName опционален. Для PATCH поля со значением <c>null</c> не изменяются.
/// </summary>
/// <param name="CodeName">Системное имя учёной степени.</param>
/// <param name="DisplayName">Отображаемое имя учёной степени.</param>
/// <param name="ShortName">Сокращённое название (опционально). null или пустая строка — очистить.</param>
public sealed record UpsertAcademicDegreeCommand(
    string? CodeName,
    string? DisplayName,
    string? ShortName);

/// <summary>
/// Типы ошибок при работе с учёными степенями.
/// </summary>
public enum AcademicDegreesError
{
    /// <summary>
    /// Ошибка валидации входных данных.
    /// </summary>
    Validation,

    /// <summary>
    /// Учёная степень не найдена по указанному идентификатору.
    /// </summary>
    NotFound,

    /// <summary>
    /// Конфликт: учёная степень с таким именем уже существует.
    /// </summary>
    Conflict,
}
