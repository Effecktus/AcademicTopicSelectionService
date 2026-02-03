namespace DirectoryOfGraduates.Application.Dictionaries.UserRoles;

public interface IUserRolesService
{
    Task<PagedResult<UserRoleDto>> ListAsync(ListUserRolesQuery query, CancellationToken ct);
    Task<UserRoleDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<UserRoleDto>> CreateAsync(UpsertUserRoleCommand command, CancellationToken ct);
    Task<Result<UserRoleDto>> UpdateAsync(Guid id, UpsertUserRoleCommand command, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

