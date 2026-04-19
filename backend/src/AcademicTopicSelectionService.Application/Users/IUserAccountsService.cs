using AcademicTopicSelectionService.Application.Auth;
using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Users;

/// <summary>
/// Создание учётных записей администратором (без выдачи JWT).
/// </summary>
public interface IUserAccountsService
{
    /// <summary>
    /// Создаёт пользователя с заданной ролью. Вход — через <see cref="IAuthService.LoginAsync"/>.
    /// </summary>
    Task<Result<CreatedUserDto, AuthError>> CreateAsync(CreateUserRequest request, CancellationToken ct);
}
