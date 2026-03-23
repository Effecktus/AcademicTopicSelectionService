using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Dictionaries.UserRoles;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Репозиторий для работы с ролями пользователей в базе данных.
/// </summary>
public interface IUserRolesRepository
{
    /// <summary>
    /// Получает постраничный список ролей с возможностью поиска.
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
    Task<UserRoleDto?> GetAsync(Guid id, CancellationToken ct);
    
    /// <summary>
    /// Проверяет существование роли с указанным именем.
    /// </summary>
    /// <param name="name">Системное имя роли для проверки.</param>
    /// <param name="excludeId">Идентификатор роли, которую нужно исключить из проверки (для обновления).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если роль с таким именем существует.</returns>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId, CancellationToken ct);
    
    /// <summary>
    /// Создаёт новую роль.
    /// </summary>
    /// <param name="name">Системное имя роли.</param>
    /// <param name="displayName">Отображаемое имя роли.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Созданная роль с присвоенным идентификатором.</returns>
    Task<UserRoleDto> CreateAsync(string name, string displayName, CancellationToken ct);
    
    /// <summary>
    /// Полностью обновляет роль (PUT).
    /// </summary>
    /// <param name="id">Идентификатор роли.</param>
    /// <param name="name">Новое системное имя.</param>
    /// <param name="displayName">Новое отображаемое имя.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Обновлённая роль или <c>null</c>, если не найдена.</returns>
    Task<UserRoleDto?> UpdateAsync(Guid id, string name, string displayName, CancellationToken ct);
    
    /// <summary>
    /// Частично обновляет роль (PATCH). Поля со значением <c>null</c> не изменяются.
    /// </summary>
    /// <param name="id">Идентификатор роли.</param>
    /// <param name="name">Новое системное имя или <c>null</c> для сохранения текущего.</param>
    /// <param name="displayName">Новое отображаемое имя или <c>null</c> для сохранения текущего.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Обновлённая роль или <c>null</c>, если не найдена.</returns>
    Task<UserRoleDto?> PatchAsync(Guid id, string? name, string? displayName, CancellationToken ct);
    
    /// <summary>
    /// Удаляет роль по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор роли.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns><c>true</c>, если роль была удалена; <c>false</c>, если не найдена.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

