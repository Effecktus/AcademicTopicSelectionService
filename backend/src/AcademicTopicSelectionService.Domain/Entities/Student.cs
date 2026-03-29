using AcademicTopicSelectionService.Domain.Common;

namespace AcademicTopicSelectionService.Domain.Entities;

/// <summary>
/// Таблица студентов. Содержит дополнительную информацию о студентах: принадлежность к учебной группе.
/// </summary>
public partial class Student : IAuditableEntity
{
    /// <summary>
    /// Уникальный идентификатор студента.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя-студента (внешний ключ к таблице Users).
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Идентификатор учебной группы студента (внешний ключ к таблице StudyGroups).
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Дата и время создания записи о студенте.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время последнего обновления записи о студенте.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<GraduateWork> GraduateWorks { get; set; } = new List<GraduateWork>();

    public virtual ICollection<StudentApplication> StudentApplications { get; set; } = new List<StudentApplication>();

    public virtual User User { get; set; } = null!;

    public virtual StudyGroup Group { get; set; } = null!;
}
