namespace AcademicTopicSelectionService.Application.Dictionaries.AcademicDegrees;

/// <summary>
/// Сервис бизнес-логики для работы с учёными степенями.
/// Выполняет валидацию, проверку на уникальность и делегирует операции репозиторию.
/// </summary>
public interface IAcademicDegreesService
{
    /// <summary>
    /// Получает постраничный список учёных степеней с нормализацией параметров запроса.
    /// </summary>
    Task<PagedResult<AcademicDegreeDto>> ListAsync(ListAcademicDegreesQuery query, CancellationToken ct);

    /// <summary>
    /// Получает учёную степень по идентификатору.
    /// </summary>
    Task<AcademicDegreeDto?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Создаёт новую учёную степень с валидацией и проверкой уникальности имени.
    /// </summary>
    Task<Result<AcademicDegreeDto, AcademicDegreesError>> CreateAsync(
        UpsertAcademicDegreeCommand command, CancellationToken ct);

    /// <summary>
    /// Полностью обновляет учёную степень (PUT) с валидацией и проверкой уникальности имени.
    /// </summary>
    Task<Result<AcademicDegreeDto, AcademicDegreesError>> UpdateAsync(
        Guid id, UpsertAcademicDegreeCommand command, CancellationToken ct);

    /// <summary>
    /// Частично обновляет учёную степень (PATCH). Поля со значением <c>null</c> не изменяются.
    /// </summary>
    Task<Result<AcademicDegreeDto, AcademicDegreesError>> PatchAsync(
        Guid id, UpsertAcademicDegreeCommand command, CancellationToken ct);

    /// <summary>
    /// Удаляет учёную степень по идентификатору.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
