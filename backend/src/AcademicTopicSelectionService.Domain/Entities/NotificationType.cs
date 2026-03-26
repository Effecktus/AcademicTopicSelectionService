using AcademicTopicSelectionService.Domain.Common;

namespace AcademicTopicSelectionService.Domain.Entities;

/// <summary>
/// Справочник типов уведомлений системы. Содержит системные и отображаемые названия типов.
/// </summary>
public partial class NotificationType : IAuditableEntity
{
    /// <summary>
    /// Уникальный идентификатор типа уведомления
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Системное значение типа (для кода), регистронезависимо
    /// </summary>
    public string CodeName { get; set; } = null!;

    /// <summary>
    /// Отображаемое значение типа (для пользовательского интерфейса)
    /// </summary>
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// Дата и время создания записи о типе уведомления
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время последнего обновления записи о типе уведомления
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
