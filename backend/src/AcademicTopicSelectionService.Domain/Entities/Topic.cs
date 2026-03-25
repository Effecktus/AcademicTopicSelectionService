using AcademicTopicSelectionService.Domain.Common;

namespace AcademicTopicSelectionService.Domain.Entities;

/// <summary>
/// Таблица тем выпускных квалификационных работ (ВКР). Содержит информацию о темах, предлагаемых преподавателями для студентов.
/// </summary>
public partial class Topic : IAuditableEntity
{
    /// <summary>
    /// Уникальный идентификатор темы ВКР
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Название темы выпускной квалификационной работы
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// Подробное описание темы ВКР, требования и особенности
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Учебный год, для которого предназначена тема
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Идентификатор преподавателя, предложившего тему (внешний ключ к таблице Teachers)
    /// </summary>
    public Guid TeacherId { get; set; }

    /// <summary>
    /// Идентификатор статуса темы (внешний ключ к таблице TopicStatuses)
    /// </summary>
    public Guid StatusId { get; set; }

    /// <summary>
    /// Дата и время создания записи о теме
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время последнего обновления записи о теме
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual TopicStatus Status { get; set; } = null!;

    public virtual ICollection<StudentApplication> StudentApplications { get; set; } = new List<StudentApplication>();

    public virtual Teacher Teacher { get; set; } = null!;
}
