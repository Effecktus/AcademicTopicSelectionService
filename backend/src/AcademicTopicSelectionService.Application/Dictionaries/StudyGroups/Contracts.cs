namespace AcademicTopicSelectionService.Application.Dictionaries.StudyGroups;

/// <summary>
/// DTO учебной группы для передачи данных между слоями приложения.
/// </summary>
/// <param name="Id">Уникальный идентификатор группы.</param>
/// <param name="CodeName">Номер группы (4-значное целое число, 1000–9999).</param>
/// <param name="CreatedAt">Дата и время создания записи (UTC).</param>
/// <param name="UpdatedAt">Дата и время последнего обновления (UTC), null если не обновлялась.</param>
public sealed record StudyGroupDto(
    Guid Id,
    int CodeName,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// Запрос на получение списка групп с пагинацией и опциональным фильтром по номеру.
/// </summary>
/// <param name="CodeName">Точный номер группы для фильтрации (опционально).</param>
/// <param name="Page">Номер страницы (начиная с 1).</param>
/// <param name="PageSize">Количество элементов на странице (1–200).</param>
public sealed record ListStudyGroupsQuery(
    int? CodeName,
    int Page = 1,
    int PageSize = 50);

/// <summary>
/// Команда для создания, полного (PUT) или частичного (PATCH) обновления группы.
/// Для PATCH поле со значением <c>null</c> не изменяется.
/// </summary>
/// <param name="CodeName">Номер группы (4-значное целое число, 1000–9999).</param>
public sealed record UpsertStudyGroupCommand(int? CodeName);

/// <summary>
/// Типы ошибок при работе с учебными группами.
/// </summary>
public enum StudyGroupsError
{
    /// <summary>
    /// Ошибка валидации входных данных.
    /// </summary>
    Validation,

    /// <summary>
    /// Группа не найдена по указанному идентификатору.
    /// </summary>
    NotFound,

    /// <summary>
    /// Конфликт: группа с таким номером уже существует.
    /// </summary>
    Conflict
}
