using DirectoryOfGraduates.Application.Abstractions;

namespace DirectoryOfGraduates.Application.Dictionaries.UserRoles;

/// <summary>
/// Реализация сервиса бизнес-логики для работы с ролями пользователей.
/// </summary>
/// <remarks>
/// Выполняет валидацию входных данных, проверку уникальности имён ролей
/// и делегирует операции с данными репозиторию.
/// </remarks>
public sealed class UserRolesService : IUserRolesService
{
    private readonly IUserRolesRepository _repo;

    /// <summary>
    /// Создаёт новый экземпляр сервиса.
    /// </summary>
    /// <param name="repo">Репозиторий для работы с данными ролей.</param>
    public UserRolesService(IUserRolesRepository repo)
    {
        _repo = repo;
    }

    /// <inheritdoc />
    public Task<PagedResult<UserRoleDto>> ListAsync(ListUserRolesQuery query, CancellationToken ct)
    {
        // Нормализуем параметры запроса
        var normalized = query with
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 200),
            Q = string.IsNullOrWhiteSpace(query.Q) ? null : query.Q.Trim()
        };

        return _repo.ListAsync(normalized, ct);
    }

    /// <inheritdoc />
    public Task<UserRoleDto?> GetByIdAsync(Guid id, CancellationToken ct) => _repo.GetByIdAsync(id, ct);

    /// <inheritdoc />
    public async Task<Result<UserRoleDto>> CreateAsync(UpsertUserRoleCommand command, CancellationToken ct)
    {
        var (ok, name, displayName, error) = Validate(command);
        if (!ok) return Result<UserRoleDto>.Fail(UserRolesError.Validation, error);

        // Проверяем уникальность имени
        if (await _repo.ExistsByNameAsync(name, excludeId: null, ct))
        {
            return Result<UserRoleDto>.Fail(UserRolesError.Conflict, "UserRole with the same Name already exists");
        }

        var created = await _repo.CreateAsync(name, displayName, ct);
        return Result<UserRoleDto>.Ok(created);
    }

    /// <inheritdoc />
    public async Task<Result<UserRoleDto>> UpdateAsync(Guid id, UpsertUserRoleCommand command, CancellationToken ct)
    {
        var (ok, name, displayName, error) = Validate(command);
        if (!ok) return Result<UserRoleDto>.Fail(UserRolesError.Validation, error);

        // Проверяем уникальность имени (исключая текущую запись)
        if (await _repo.ExistsByNameAsync(name, excludeId: id, ct))
        {
            return Result<UserRoleDto>.Fail(UserRolesError.Conflict, "UserRole with the same Name already exists");
        }

        var updated = await _repo.UpdateAsync(id, name, displayName, ct);
        if (updated is null)
            return Result<UserRoleDto>.Fail(UserRolesError.NotFound, "UserRole not found");

        return Result<UserRoleDto>.Ok(updated);
    }

    /// <inheritdoc />
    public async Task<Result<UserRoleDto>> PatchAsync(Guid id, PatchUserRoleCommand command, CancellationToken ct)
    {
        var (ok, name, displayName, error) = ValidatePatch(command);
        if (!ok) return Result<UserRoleDto>.Fail(UserRolesError.Validation, error);

        // Если name передан, проверяем уникальность (исключая текущую запись)
        if (name is not null && await _repo.ExistsByNameAsync(name, excludeId: id, ct))
        {
            return Result<UserRoleDto>.Fail(UserRolesError.Conflict, "UserRole with the same Name already exists");
        }

        var patched = await _repo.PatchAsync(id, name, displayName, ct);
        if (patched is null)
            return Result<UserRoleDto>.Fail(UserRolesError.NotFound, "UserRole not found");

        return Result<UserRoleDto>.Ok(patched);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => _repo.DeleteAsync(id, ct);

    /// <summary>
    /// Валидирует команду создания/полного обновления роли.
    /// </summary>
    /// <param name="command">Команда для валидации.</param>
    /// <returns>Кортеж с результатом валидации и нормализованными значениями.</returns>
    private static (bool ok, string name, string displayName, string error) Validate(UpsertUserRoleCommand command)
    {
        var name = (command.Name ?? string.Empty).Trim();
        var displayName = (command.DisplayName ?? string.Empty).Trim();

        if (name.Length == 0) return (false, "", "", "Name is required");
        if (displayName.Length == 0) return (false, "", "", "DisplayName is required");
        if (displayName.Length > 100) return (false, "", "", "DisplayName must be <= 100 chars");

        return (true, name, displayName, string.Empty);
    }

    /// <summary>
    /// Валидация для PATCH: поля необязательны, но если переданы — должны быть валидны.
    /// </summary>
    private static (bool ok, string? name, string? displayName, string error) ValidatePatch(PatchUserRoleCommand command)
    {
        string? name = null;
        string? displayName = null;

        if (command.Name is not null)
        {
            name = command.Name.Trim();
            if (name.Length == 0)
                return (false, null, null, "Name cannot be empty if provided");
        }

        if (command.DisplayName is not null)
        {
            displayName = command.DisplayName.Trim();
            if (displayName.Length == 0)
                return (false, null, null, "DisplayName cannot be empty if provided");
            if (displayName.Length > 100)
                return (false, null, null, "DisplayName must be <= 100 chars");
        }

        // Должно быть хотя бы одно поле для обновления
        if (name is null && displayName is null)
            return (false, null, null, "At least one field must be provided for patch");

        return (true, name, displayName, string.Empty);
    }
}

