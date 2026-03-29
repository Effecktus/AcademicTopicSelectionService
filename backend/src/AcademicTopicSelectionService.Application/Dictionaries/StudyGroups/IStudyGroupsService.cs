using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Dictionaries.StudyGroups;

/// <summary>
/// Сервис бизнес-логики для работы с учебными группами.
/// Выполняет валидацию, проверку на уникальность и делегирует операции репозиторию.
/// </summary>
public interface IStudyGroupsService
{
    /// <summary>
    /// Получает постраничный список групп с нормализацией параметров запроса.
    /// </summary>
    /// <param name="query">Параметры запроса (фильтр, пагинация).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Постраничный результат со списком групп.</returns>
    Task<PagedResult<StudyGroupDto>> ListAsync(ListStudyGroupsQuery query, CancellationToken ct);

    /// <summary>
    /// Получает группу по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор группы.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Группа или <c>null</c>, если не найдена.</returns>
    Task<StudyGroupDto?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Создаёт новую группу с валидацией и проверкой уникальности номера.
    /// </summary>
    /// <param name="command">Данные для создания группы.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат операции: созданная группа или ошибка.</returns>
    Task<Result<StudyGroupDto, StudyGroupsError>> CreateAsync(UpsertStudyGroupCommand command, CancellationToken ct);

    /// <summary>
    /// Полностью обновляет группу (PUT) с валидацией и проверкой уникальности номера.
    /// </summary>
    /// <param name="id">Идентификатор группы.</param>
    /// <param name="command">Новые данные группы.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат операции: обновлённая группа или ошибка.</returns>
    Task<Result<StudyGroupDto, StudyGroupsError>> UpdateAsync(Guid id, UpsertStudyGroupCommand command, CancellationToken ct);

    /// <summary>
    /// Частично обновляет группу (PATCH). Поля со значением <c>null</c> не изменяются.
    /// </summary>
    /// <param name="id">Идентификатор группы.</param>
    /// <param name="command">Данные для частичного обновления.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат операции: обновлённая группа или ошибка.</returns>
    Task<Result<StudyGroupDto, StudyGroupsError>> PatchAsync(Guid id, UpsertStudyGroupCommand command, CancellationToken ct);

    /// <summary>
    /// Удаляет группу по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор группы.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если группа была удалена; <c>false</c>, если не найдена.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
