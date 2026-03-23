using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.AcademicDegrees;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Репозиторий для работы с учёными степенями в базе данных.
/// </summary>
public interface IAcademicDegreesRepository
{
    /// <summary>
    /// Получает постраничный список учёных степеней с возможностью поиска.
    /// </summary>
    Task<PagedResult<AcademicDegreeDto>> ListAsync(ListAcademicDegreesQuery query, CancellationToken ct);

    /// <summary>
    /// Получает учёную степень по идентификатору.
    /// </summary>
    Task<AcademicDegreeDto?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Проверяет существование учёной степени с указанным именем.
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct);

    /// <summary>
    /// Создаёт новую учёную степень.
    /// </summary>
    Task<AcademicDegreeDto> CreateAsync(string name, string displayName, string? shortName, CancellationToken ct);

    /// <summary>
    /// Полностью обновляет учёную степень (PUT).
    /// </summary>
    Task<AcademicDegreeDto?> UpdateAsync(Guid id, string name, string displayName, string? shortName, CancellationToken ct);

    /// <summary>
    /// Частично обновляет учёную степень (PATCH). Поля со значением <c>null</c> не изменяются.
    /// </summary>
    Task<AcademicDegreeDto?> PatchAsync(Guid id, string? name, string? displayName, string? shortName, CancellationToken ct);

    /// <summary>
    /// Удаляет учёную степень по идентификатору.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
