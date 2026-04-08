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
