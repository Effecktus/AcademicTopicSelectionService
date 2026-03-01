namespace DirectoryOfGraduates.Application.Dictionaries.Positions;

/// <summary>
/// Сервис бизнес-логики для работы с должностями преподавателей.
/// Выполняет валидацию, проверку на уникальность и делегирует операции репозиторию.
/// </summary>
public interface IPositionsService
{
    /// <summary>
    /// Получает постраничный список должностей с нормализацией параметров запроса.
    /// </summary>
    Task<PagedResult<PositionDto>> ListAsync(ListPositionsQuery query, CancellationToken ct);

    /// <summary>
    /// Получает должность по идентификатору.
    /// </summary>
    Task<PositionDto?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Создаёт новую должность с валидацией и проверкой уникальности имени.
    /// </summary>
    Task<Result<PositionDto, PositionsError>> CreateAsync(UpsertPositionCommand command, CancellationToken ct);

    /// <summary>
    /// Полностью обновляет должность (PUT) с валидацией и проверкой уникальности имени.
    /// </summary>
    Task<Result<PositionDto, PositionsError>> UpdateAsync(Guid id, UpsertPositionCommand command, CancellationToken ct);

    /// <summary>
    /// Частично обновляет должность (PATCH). Поля со значением <c>null</c> не изменяются.
    /// </summary>
    Task<Result<PositionDto, PositionsError>> PatchAsync(Guid id, UpsertPositionCommand command, CancellationToken ct);

    /// <summary>
    /// Удаляет должность по идентификатору.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
