using DirectoryOfGraduates.Application.Abstractions;

namespace DirectoryOfGraduates.Application.Dictionaries.UserRoles;

public sealed class UserRolesService : IUserRolesService
{
    private readonly IUserRolesRepository _repo;

    public UserRolesService(IUserRolesRepository repo)
    {
        _repo = repo;
    }

    public Task<PagedResult<UserRoleDto>> ListAsync(ListUserRolesQuery query, CancellationToken ct)
    {
        var normalized = query with
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 200),
            Q = string.IsNullOrWhiteSpace(query.Q) ? null : query.Q.Trim()
        };

        return _repo.ListAsync(normalized, ct);
    }

    public Task<UserRoleDto?> GetByIdAsync(Guid id, CancellationToken ct) => _repo.GetByIdAsync(id, ct);

    public async Task<Result<UserRoleDto>> CreateAsync(UpsertUserRoleCommand command, CancellationToken ct)
    {
        var (ok, name, displayName, error) = Validate(command);
        if (!ok) return Result<UserRoleDto>.Fail(UserRolesError.Validation, error);

        if (await _repo.ExistsByNameAsync(name, excludeId: null, ct))
        {
            return Result<UserRoleDto>.Fail(UserRolesError.Conflict, "UserRole with the same Name already exists");
        }

        var created = await _repo.CreateAsync(name, displayName, ct);
        return Result<UserRoleDto>.Ok(created);
    }

    public async Task<Result<UserRoleDto>> UpdateAsync(Guid id, UpsertUserRoleCommand command, CancellationToken ct)
    {
        var (ok, name, displayName, error) = Validate(command);
        if (!ok) return Result<UserRoleDto>.Fail(UserRolesError.Validation, error);

        if (await _repo.ExistsByNameAsync(name, excludeId: id, ct))
        {
            return Result<UserRoleDto>.Fail(UserRolesError.Conflict, "UserRole with the same Name already exists");
        }

        var updated = await _repo.UpdateAsync(id, name, displayName, ct);
        if (updated is null)
            return Result<UserRoleDto>.Fail(UserRolesError.NotFound, "UserRole not found");

        return Result<UserRoleDto>.Ok(updated);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => _repo.DeleteAsync(id, ct);

    private static (bool ok, string name, string displayName, string error) Validate(UpsertUserRoleCommand command)
    {
        var name = (command.Name ?? string.Empty).Trim();
        var displayName = (command.DisplayName ?? string.Empty).Trim();

        if (name.Length == 0) return (false, "", "", "Name is required");
        if (displayName.Length == 0) return (false, "", "", "DisplayName is required");
        if (displayName.Length > 100) return (false, "", "", "DisplayName must be <= 100 chars");

        return (true, name, displayName, string.Empty);
    }
}

