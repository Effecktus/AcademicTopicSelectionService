namespace AcademicTopicSelectionService.Application.Dictionaries.TopicCreatorTypes;

/// <summary>
/// DTO типа создателя темы ВКР для передачи данных между слоями приложения.
/// </summary>
/// <param name="Id">Уникальный идентификатор типа.</param>
/// <param name="CodeName">Системное имя типа.</param>
/// <param name="DisplayName">Отображаемое имя типа.</param>
/// <param name="CreatedAt">Дата и время создания записи (UTC).</param>
/// <param name="UpdatedAt">Дата и время последнего обновления (UTC), null если не обновлялась.</param>
public sealed record TopicCreatorTypeDto(
    Guid Id,
    string CodeName,
    string DisplayName,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
    : NamedDictionaryItemDto(Id, CodeName, DisplayName, CreatedAt, UpdatedAt);

/// <summary>
/// Запрос на получение списка типов создателей тем с пагинацией и поиском.
/// </summary>
/// <param name="Query">Строка поиска по <c>CodeName</c> и <c>DisplayName</c> (регистронезависимый ILIKE).</param>
/// <param name="Page">Номер страницы (начиная с 1).</param>
/// <param name="PageSize">Количество элементов на странице (1–200).</param>
public sealed record ListTopicCreatorTypesQuery(
    string? Query,
    int Page = 1,
    int PageSize = 50)
    : ListNamedDictionaryItemsQuery(Query, Page, PageSize);

/// <summary>
/// Команда для создания, полного (PUT) или частичного (PATCH) обновления типа создателя темы.
/// Для POST/PUT оба поля обязательны. Для PATCH поля со значением <c>null</c> не изменяются.
/// </summary>
/// <param name="CodeName">Системное имя типа.</param>
/// <param name="DisplayName">Отображаемое имя типа.</param>
public sealed record UpsertTopicCreatorTypeCommand(
    string? CodeName,
    string? DisplayName)
    : UpsertNamedDictionaryItemCommand(CodeName, DisplayName);

/// <summary>
/// Типы ошибок при работе с типами создателей тем ВКР.
/// </summary>
public enum TopicCreatorTypesError
{
    /// <summary>
    /// Ошибка валидации входных данных.
    /// </summary>
    Validation,

    /// <summary>
    /// Тип создателя темы не найден по указанному идентификатору.
    /// </summary>
    NotFound,

    /// <summary>
    /// Конфликт: тип с таким именем уже существует.
    /// </summary>
    Conflict,
}
