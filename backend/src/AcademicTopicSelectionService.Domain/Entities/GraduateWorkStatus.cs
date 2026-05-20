using AcademicTopicSelectionService.Domain.Common;

namespace AcademicTopicSelectionService.Domain.Entities;

/// <summary>
/// Справочник статусов выпускных квалификационных работ.
/// </summary>
public partial class GraduateWorkStatus : IAuditableEntity
{
    /// <summary>
    /// Уникальный идентификатор статуса ВКР
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

    public virtual ICollection<GraduateWork> GraduateWorks { get; set; } = new List<GraduateWork>();
}
