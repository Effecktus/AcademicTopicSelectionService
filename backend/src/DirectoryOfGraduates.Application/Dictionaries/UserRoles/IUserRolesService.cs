namespace DirectoryOfGraduates.Application.Dictionaries.UserRoles;

/// <summary>
/// Сервис бизнес-логики для работы с ролями пользователей.
/// Выполняет валидацию, проверку на уникальность и делегирует операции репозиторию.
/// </summary>
public interface IUserRolesService
{
    /// <summary>
    /// Получает постраничный список ролей с нормализацией параметров запроса.
    /// </summary>
    /// <param name="query">Параметры запроса (поиск, пагинация).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Постраничный результат со списком ролей.</returns>
    Task<PagedResult<UserRoleDto>> ListAsync(ListUserRolesQuery query, CancellationToken ct);
    
    /// <summary>
    /// Получает роль по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор роли.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Роль или <c>null</c>, если не найдена.</returns>
    Task<UserRoleDto?> GetByIdAsync(Guid id, CancellationToken ct);
    
    /// <summary>
    /// Создаёт новую роль с валидацией и проверкой уникальности имени.
    /// </summary>
    /// <param name="command">Данные для создания роли.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат операции: созданная роль или ошибка.</returns>
    Task<Result<UserRoleDto>> CreateAsync(UpsertUserRoleCommand command, CancellationToken ct);
    
    /// <summary>
    /// Полностью обновляет роль (PUT) с валидацией и проверкой уникальности имени.
    /// </summary>
    /// <param name="id">Идентификатор роли.</param>
    /// <param name="command">Новые данные роли.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат операции: обновлённая роль или ошибка.</returns>
    Task<Result<UserRoleDto>> UpdateAsync(Guid id, UpsertUserRoleCommand command, CancellationToken ct);
    
    /// <summary>
    /// Частично обновляет роль (PATCH). Поля со значением <c>null</c> не изменяются.
    /// </summary>
    /// <param name="id">Идентификатор роли.</param>
    /// <param name="command">Данные для частичного обновления.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Результат операции: обновлённая роль или ошибка.</returns>
    Task<Result<UserRoleDto>> PatchAsync(Guid id, PatchUserRoleCommand command, CancellationToken ct);
    
    /// <summary>
    /// Удаляет роль по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор роли.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если роль была удалена; <c>false</c>, если не найдена.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

