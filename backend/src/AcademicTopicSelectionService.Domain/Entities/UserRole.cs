using AcademicTopicSelectionService.Domain.Common;

namespace AcademicTopicSelectionService.Domain.Entities;

/// <summary>
/// Справочник ролей пользователей системы. Содержит системные и отображаемые названия ролей.
/// </summary>
public partial class UserRole : IAuditableEntity
{
    /// <summary>
    /// Уникальный идентификатор роли пользователя
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Системное значение роли (для кода), регистронезависимо
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Отображаемое значение роли (для пользовательского интерфейса)
    /// </summary>
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// Дата и время создания записи о роли
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время последнего обновления записи о роли
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
