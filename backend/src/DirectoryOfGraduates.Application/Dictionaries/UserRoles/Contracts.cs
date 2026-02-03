namespace DirectoryOfGraduates.Application.Dictionaries.UserRoles;

public sealed record UserRoleDto(Guid Id, string Name, string DisplayName, DateTime CreatedAt, DateTime? UpdatedAt);

public sealed record ListUserRolesQuery(string? Q, int Page = 1, int PageSize = 50);

public sealed record UpsertUserRoleCommand(string? Name, string? DisplayName);

public sealed record PagedResult<T>(int Page, int PageSize, long Total, IReadOnlyList<T> Items);

public enum UserRolesError
{
    Validation,
    NotFound,
    Conflict
}

public sealed record Result<T>(T? Value, UserRolesError? Error, string Message)
{
    public static Result<T> Ok(T value) => new(value, null, string.Empty);
    public static Result<T> Fail(UserRolesError error, string message) => new(default, error, message);
}

