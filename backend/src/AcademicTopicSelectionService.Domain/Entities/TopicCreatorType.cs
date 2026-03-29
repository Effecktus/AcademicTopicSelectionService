using AcademicTopicSelectionService.Domain.Common;

namespace AcademicTopicSelectionService.Domain.Entities;

/// <summary>
/// Справочник типов пользователей, создающих темы ВКР.
/// Определяет, кем была предложена тема: научным руководителем или студентом.
/// </summary>
public partial class TopicCreatorType : IAuditableEntity
{
    /// <summary>
    /// Уникальный идентификатор типа создателя темы
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
    /// Дата и время создания записи
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время последнего обновления записи
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Topic> Topics { get; set; } = new List<Topic>();
}
