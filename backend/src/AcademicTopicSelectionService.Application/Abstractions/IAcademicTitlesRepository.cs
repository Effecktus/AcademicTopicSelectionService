<<<<<<< HEAD:backend/src/DirectoryOfGraduates.Application/Abstractions/IAcademicTitlesRepository.cs
using DirectoryOfGraduates.Application.Dictionaries;
using DirectoryOfGraduates.Application.Dictionaries.AcademicTitles;

namespace DirectoryOfGraduates.Application.Abstractions;
=======
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.AcademicTitles;

namespace AcademicTopicSelectionService.Application.Abstractions;
>>>>>>> developer:backend/src/AcademicTopicSelectionService.Application/Abstractions/IAcademicTitlesRepository.cs

/// <summary>
/// Репозиторий для работы с учёными званиями в базе данных.
/// </summary>
public interface IAcademicTitlesRepository
{
    /// <summary>
    /// Получает постраничный список учёных званий с возможностью поиска.
    /// </summary>
    Task<PagedResult<AcademicTitleDto>> ListAsync(ListAcademicTitlesQuery query, CancellationToken ct);

    /// <summary>
    /// Получает учёное звание по идентификатору.
    /// </summary>
    Task<AcademicTitleDto?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Проверяет существование учёного звания с указанным именем.
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct);

    /// <summary>
    /// Создаёт новое учёное звание.
    /// </summary>
    Task<AcademicTitleDto> CreateAsync(string name, string displayName, CancellationToken ct);

    /// <summary>
    /// Полностью обновляет учёное звание (PUT).
    /// </summary>
    Task<AcademicTitleDto?> UpdateAsync(Guid id, string name, string displayName, CancellationToken ct);

    /// <summary>
    /// Частично обновляет учёное звание (PATCH). Поля со значением <c>null</c> не изменяются.
    /// </summary>
    Task<AcademicTitleDto?> PatchAsync(Guid id, string? name, string? displayName, CancellationToken ct);

    /// <summary>
    /// Удаляет учёное звание по идентификатору.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
