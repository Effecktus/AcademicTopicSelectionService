using AcademicTopicSelectionService.Domain.Common;

namespace AcademicTopicSelectionService.Domain.Entities;

/// <summary>
/// Справочник статусов действий по заявкам на темы ВКР.
/// Определяет результат рассмотрения: на согласовании, согласовано, отклонено или отменено.
/// </summary>
public partial class ApplicationActionStatus : IAuditableEntity
{
    /// <summary>
    /// Уникальный идентификатор статуса действия
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Системное значение статуса (для кода), регистронезависимо
    /// </summary>
    public string CodeName { get; set; } = null!;

    /// <summary>
    /// Отображаемое значение статуса (для пользовательского интерфейса)
    /// </summary>
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// Дата и время создания записи о статусе
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время последнего обновления записи о статусе
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ApplicationAction> ApplicationActions { get; set; } = new List<ApplicationAction>();
}
