using AcademicTopicSelectionService.Application.Auth;
using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Users;

/// <summary>
/// Создание и управление учётными записями администратором (без выдачи JWT).
/// </summary>
public interface IUserAccountsService
{
    /// <summary>
    /// Создаёт пользователя с заданной ролью. Вход — через <see cref="IAuthService.LoginAsync"/>.
    /// </summary>
    Task<Result<CreatedUserDto, AuthError>> CreateAsync(CreateUserRequest request, CancellationToken ct);

    /// <summary>
    /// Возвращает постраничный список пользователей с фильтрацией по роли и поиском по email/ФИО.
    /// </summary>
    Task<PagedResult<UserListItemDto>> ListAsync(ListUsersQuery query, CancellationToken ct);
}
