using AcademicTopicSelectionService.Domain.Entities;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Абстракция репозитория пользователей.
/// </summary>
public interface IUsersRepository
{
    /// <summary>
    /// Возвращает пользователя по email с загруженной ролью (<c>Role</c>).
    /// Поиск регистронезависимый (CITEXT).
    /// </summary>
    /// <param name="email">Email пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Пользователь или <c>null</c>.</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);

    /// <summary>
    /// Проверяет, существует ли пользователь с указанным email.
    /// </summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct);

    /// <summary>
    /// Возвращает пользователя по идентификатору с загруженной ролью (<c>Role</c>).
    /// </summary>
    /// <param name="id">Идентификатор пользователя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Пользователь или <c>null</c>.</returns>
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Создаёт пользователя и возвращает его с загруженной навигацией <c>Role</c>.
    /// </summary>
    /// <param name="user">Новый пользователь (без Id, CreatedAt — проставятся БД).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Сохранённый пользователь с навигацией <c>Role</c>.</returns>
    Task<User> CreateAsync(User user, CancellationToken ct);
}
