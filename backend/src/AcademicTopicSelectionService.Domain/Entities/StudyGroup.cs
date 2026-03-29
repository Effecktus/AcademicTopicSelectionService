using AcademicTopicSelectionService.Domain.Common;

namespace AcademicTopicSelectionService.Domain.Entities;

/// <summary>
/// Справочник учебных групп.
/// Содержит номер группы в формате XXXX (факультет, курс, номер).
/// </summary>
public partial class StudyGroup : IAuditableEntity
{
    /// <summary>
    /// Уникальный идентификатор группы.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Номер учебной группы (формат: XXXX, например 4411).
    /// Значение от 1000 до 9999.
    /// </summary>
    public int CodeName { get; set; }

    /// <summary>
    /// Дата и время создания записи о группе.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время последнего обновления записи о группе.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
