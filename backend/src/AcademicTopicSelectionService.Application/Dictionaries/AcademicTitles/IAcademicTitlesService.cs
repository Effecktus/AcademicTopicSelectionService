namespace AcademicTopicSelectionService.Application.Dictionaries.AcademicTitles;

/// <summary>
/// Сервис бизнес-логики для работы с учёными званиями.
/// Выполняет валидацию, проверку на уникальность и делегирует операции репозиторию.
/// </summary>
public interface IAcademicTitlesService
{
    /// <summary>
    /// Получает постраничный список учёных званий с нормализацией параметров запроса.
    /// </summary>
    Task<PagedResult<AcademicTitleDto>> ListAsync(ListAcademicTitlesQuery query, CancellationToken ct);

    /// <summary>
    /// Получает учёное звание по идентификатору.
    /// </summary>
    Task<AcademicTitleDto?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Создаёт новое учёное звание с валидацией и проверкой уникальности имени.
    /// </summary>
    Task<Result<AcademicTitleDto, AcademicTitlesError>> CreateAsync(
        UpsertAcademicTitleCommand command, CancellationToken ct);

    /// <summary>
    /// Полностью обновляет учёное звание (PUT) с валидацией и проверкой уникальности имени.
    /// </summary>
    Task<Result<AcademicTitleDto, AcademicTitlesError>> UpdateAsync(
        Guid id, UpsertAcademicTitleCommand command, CancellationToken ct);

    /// <summary>
    /// Частично обновляет учёное звание (PATCH). Поля со значением <c>null</c> не изменяются.
    /// </summary>
    Task<Result<AcademicTitleDto, AcademicTitlesError>> PatchAsync(
        Guid id, UpsertAcademicTitleCommand command, CancellationToken ct);

    /// <summary>
    /// Удаляет учёное звание по идентификатору.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
