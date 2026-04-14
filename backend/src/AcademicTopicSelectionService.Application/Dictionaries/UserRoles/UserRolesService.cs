using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Dictionaries.UserRoles;

/// <inheritdoc />
/// <param name="repo">Репозиторий для работы с данными ролей.</param>
public sealed class UserRolesService(IUserRolesRepository repo) : IUserRolesService
{
    /// <inheritdoc />
    public Task<PagedResult<UserRoleDto>> ListAsync(ListUserRolesQuery query, CancellationToken ct)
    {
        var normalized = query with
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 200),
            Query = string.IsNullOrWhiteSpace(query.Query) ? null : query.Query.Trim()
        };

        return repo.ListAsync(normalized, ct);
    }

    /// <inheritdoc />
    public Task<UserRoleDto?> GetAsync(Guid id, CancellationToken ct) => repo.GetAsync(id, ct);

    /// <inheritdoc />
    public async Task<Result<UserRoleDto, UserRolesError>> CreateAsync(UpsertUserRoleCommand command, 
        CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.Validate(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<UserRoleDto, UserRolesError>.Fail(UserRolesError.Validation, error);
        }

        if (await repo.ExistsByNameAsync(codeName, null, ct))
        {
            return Result<UserRoleDto, UserRolesError>.Fail(UserRolesError.Conflict, 
                "UserRole with the same CodeName already exists");
        }

        var created = await repo.CreateAsync(codeName, displayName, ct);
        return Result<UserRoleDto, UserRolesError>.Ok(created);
    }

    /// <inheritdoc />
    public async Task<Result<UserRoleDto, UserRolesError>> UpdateAsync(Guid id, UpsertUserRoleCommand command, 
        CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.Validate(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<UserRoleDto, UserRolesError>.Fail(UserRolesError.Validation, error);
        }

        if (await repo.ExistsByNameAsync(codeName, id, ct))
        {
            return Result<UserRoleDto, UserRolesError>.Fail(UserRolesError.Conflict, 
                "UserRole with the same CodeName already exists");
        }

        var updated = await repo.UpdateAsync(id, codeName, displayName, ct);
        return updated is null 
            ? Result<UserRoleDto, UserRolesError>.Fail(UserRolesError.NotFound, "UserRole not found") 
            : Result<UserRoleDto, UserRolesError>.Ok(updated);
    }

    /// <inheritdoc />
    public async Task<Result<UserRoleDto, UserRolesError>> PatchAsync(Guid id, UpsertUserRoleCommand command, CancellationToken ct)
    {
        var (ok, codeName, displayName, error) = DictionaryCodeDisplayValidator.ValidatePatch(command.CodeName, command.DisplayName);
        if (!ok)
        {
            return Result<UserRoleDto, UserRolesError>.Fail(UserRolesError.Validation, error);
        }

        if (codeName is not null && await repo.ExistsByNameAsync(codeName, id, ct))
        {
            return Result<UserRoleDto, UserRolesError>.Fail(UserRolesError.Conflict, "UserRole with the same CodeName already exists");
        }

        var patched = await repo.PatchAsync(id, codeName, displayName, ct);
        return patched is null 
            ? Result<UserRoleDto, UserRolesError>.Fail(UserRolesError.NotFound, "UserRole not found")
            : Result<UserRoleDto, UserRolesError>.Ok(patched);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => repo.DeleteAsync(id, ct);
}