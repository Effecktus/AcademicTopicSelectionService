using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.Positions;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Репозиторий для работы с должностями в базе данных.
/// </summary>
public interface IPositionsRepository
{
    /// <summary>
    /// Получает постраничный список должностей с возможностью поиска.
    /// </summary>
    Task<PagedResult<PositionDto>> ListAsync(ListPositionsQuery query, CancellationToken ct);

    /// <summary>
    /// Получает должность по идентификатору.
    /// </summary>
    Task<PositionDto?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Проверяет существование должности с указанным именем.
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct);

    /// <summary>
    /// Создаёт новую должность.
    /// </summary>
    Task<PositionDto> CreateAsync(string name, string displayName, CancellationToken ct);

    /// <summary>
    /// Полностью обновляет должность (PUT).
    /// </summary>
    Task<PositionDto?> UpdateAsync(Guid id, string name, string displayName, CancellationToken ct);

    /// <summary>
    /// Частично обновляет должность (PATCH). Поля со значением <c>null</c> не изменяются.
    /// </summary>
    Task<PositionDto?> PatchAsync(Guid id, string? name, string? displayName, CancellationToken ct);

    /// <summary>
    /// Удаляет должность по идентификатору.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
