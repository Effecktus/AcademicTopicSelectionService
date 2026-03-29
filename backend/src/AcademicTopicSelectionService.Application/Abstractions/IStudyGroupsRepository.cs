using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.StudyGroups;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Репозиторий для работы с учебными группами в базе данных.
/// </summary>
public interface IStudyGroupsRepository
{
    /// <summary>
    /// Получает постраничный список групп с возможностью фильтрации по номеру.
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
    /// Проверяет существование группы с указанным номером.
    /// </summary>
    /// <param name="codeName">Номер группы для проверки.</param>
    /// <param name="excludeId">Идентификатор группы, которую нужно исключить из проверки (для обновления).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если группа с таким номером существует.</returns>
    Task<bool> ExistsByCodeNameAsync(int codeName, Guid? excludeId, CancellationToken ct);

    /// <summary>
    /// Создаёт новую группу.
    /// </summary>
    /// <param name="codeName">Номер группы.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Созданная группа с присвоенным идентификатором.</returns>
    Task<StudyGroupDto> CreateAsync(int codeName, CancellationToken ct);

    /// <summary>
    /// Полностью обновляет группу (PUT).
    /// </summary>
    /// <param name="id">Идентификатор группы.</param>
    /// <param name="codeName">Новый номер группы.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Обновлённая группа или <c>null</c>, если не найдена.</returns>
    Task<StudyGroupDto?> UpdateAsync(Guid id, int codeName, CancellationToken ct);

    /// <summary>
    /// Частично обновляет группу (PATCH). Поля со значением <c>null</c> не изменяются.
    /// </summary>
    /// <param name="id">Идентификатор группы.</param>
    /// <param name="codeName">Новый номер группы или <c>null</c> для сохранения текущего.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Обновлённая группа или <c>null</c>, если не найдена.</returns>
    Task<StudyGroupDto?> PatchAsync(Guid id, int? codeName, CancellationToken ct);

    /// <summary>
    /// Удаляет группу по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор группы.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если группа была удалена; <c>false</c>, если не найдена.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
