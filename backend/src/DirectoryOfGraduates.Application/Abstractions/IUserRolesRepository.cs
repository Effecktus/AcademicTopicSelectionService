using DirectoryOfGraduates.Application.Dictionaries.UserRoles;

namespace DirectoryOfGraduates.Application.Abstractions;

public interface IUserRolesRepository
{
    Task<PagedResult<UserRoleDto>> ListAsync(ListUserRolesQuery query, CancellationToken ct);
    Task<UserRoleDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct);
    Task<UserRoleDto> CreateAsync(string name, string displayName, CancellationToken ct);
    Task<UserRoleDto?> UpdateAsync(Guid id, string name, string displayName, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

