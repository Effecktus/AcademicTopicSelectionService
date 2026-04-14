using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Topics;

/// <summary>
/// Тема ВКР для списка и детального просмотра.
/// </summary>
public sealed record TopicDto(
    Guid Id,
    string Title,
    string? Description,
    DictionaryItemRefDto Status,
    DictionaryItemRefDto CreatorType,
    Guid CreatedByUserId,
    string CreatedByEmail,
    string CreatedByFirstName,
    string CreatedByLastName,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// Список тем: фильтры и сортировка.
/// </summary>
/// <param name="Query">Подстрока в названии или описании (ILIKE).</param>
/// <param name="StatusCodeName">Точное совпадение <c>TopicStatuses.CodeName</c> (регистронезависимо).</param>
/// <param name="CreatedByUserId">Только темы указанного автора.</param>
/// <param name="CreatorTypeCodeName">Точное совпадение <c>TopicCreatorTypes.CodeName</c>.</param>
/// <param name="Sort">
/// <c>createdAtDesc</c> (по умолчанию), <c>createdAtAsc</c>, <c>titleAsc</c>, <c>titleDesc</c>.
/// </param>
/// <param name="Page">Номер страницы (с 1).</param>
/// <param name="PageSize">Размер страницы (1–200).</param>
public sealed record ListTopicsQuery(
    string? Query,
    string? StatusCodeName,
    Guid? CreatedByUserId,
    string? CreatorTypeCodeName,
    string? Sort,
    int Page = 1,
    int PageSize = 50);

/// <summary>
/// Команда на создание темы ВКР.
/// </summary>
/// <param name="Title">Название темы (обязательно, 1–500 символов).</param>
/// <param name="Description">Описание темы (опционально).</param>
/// <param name="CreatorTypeCodeName">Тип создателя: <c>Teacher</c> или <c>Student</c>.</param>
/// <param name="StatusCodeName">Статус темы: <c>Active</c> (по умолчанию) или <c>Inactive</c>.</param>
public sealed record CreateTopicCommand(
    string Title,
    string? Description,
    string CreatorTypeCodeName,
    string? StatusCodeName);

/// <summary>
/// Команда на полное обновление темы ВКР (PUT).
/// Все поля обязательны (кроме Description — опционально).
/// </summary>
/// <param name="Title">Новое название (обязательно, 1–500 символов).</param>
/// <param name="Description">Новое описание (опционально).</param>
/// <param name="StatusCodeName">Новый статус (обязательно).</param>
public sealed record ReplaceTopicCommand(
    string Title,
    string? Description,
    string StatusCodeName);

/// <summary>
/// Команда на частичное обновление темы ВКР (PATCH).
/// Поля со значением <c>null</c> не изменяются.
/// </summary>
/// <param name="Title">Новое название (null — не изменять).</param>
/// <param name="Description">Новое описание (null — не изменять, пустая строка — очистить).</param>
/// <param name="StatusCodeName">Новый статус (null — не изменять).</param>
public sealed record UpdateTopicCommand(
    string? Title,
    string? Description,
    string? StatusCodeName);

/// <summary>
/// Типы ошибок при работе с темами ВКР.
/// </summary>
public enum TopicsError
{
    /// <summary>
    /// Ошибка валидации входных данных.
    /// </summary>
    Validation,

    /// <summary>
    /// Тема не найдена по указанному идентификатору.
    /// </summary>
    NotFound,

    /// <summary>
    /// Нет прав на выполнение операции (не автор, не преподаватель и т.п.).
    /// </summary>
    Forbidden,
}
