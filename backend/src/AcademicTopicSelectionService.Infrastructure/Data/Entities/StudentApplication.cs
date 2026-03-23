using System;
using System.Collections.Generic;
using AcademicTopicSelectionService.Infrastructure.Data;

namespace AcademicTopicSelectionService.Infrastructure.Data.Entities;

/// <summary>
/// Таблица заявок студентов на темы ВКР. Содержит информацию о заявках: выбранные или предложенные темы, статусы обработки и временные метки действий преподавателей и заведующих кафедрой.
/// </summary>
public partial class StudentApplication : IAuditableEntity
{
    /// <summary>
    /// Уникальный идентификатор заявки
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор студента, подавшего заявку (внешний ключ к таблице Students)
    /// </summary>
    public Guid StudentId { get; set; }

    /// <summary>
    /// Идентификатор выбранной темы ВКР (внешний ключ к таблице Topics). NULL, если студент предлагает свою тему
    /// </summary>
    public Guid? TopicId { get; set; }

    /// <summary>
    /// Название предложенной темы ВКР (регистронезависимо). Используется, если студент предлагает свою тему вместо выбора существующей
    /// </summary>
    public string? ProposedTitle { get; set; }

    /// <summary>
    /// Подробное описание предложенной темы ВКР, требования и особенности
    /// </summary>
    public string? ProposedDescription { get; set; }

    /// <summary>
    /// Идентификатор текущего статуса заявки (внешний ключ к таблице StudentApplicationtatuses)
    /// </summary>
    public Guid StatusId { get; set; }

    /// <summary>
    /// Дата и время создания заявки
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время последнего обновления заявки
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Дата и время одобрения заявки преподавателем
    /// </summary>
    public DateTime? TeacherApprovedAt { get; set; }

    /// <summary>
    /// Дата и время отклонения заявки преподавателем
    /// </summary>
    public DateTime? TeacherRejectedAt { get; set; }

    /// <summary>
    /// Причина отклонения заявки преподавателем
    /// </summary>
    public string? TeacherRejectionReason { get; set; }

    /// <summary>
    /// Дата и время утверждения заявки заведующим кафедрой
    /// </summary>
    public DateTime? DepartmentHeadApprovedAt { get; set; }

    /// <summary>
    /// Дата и время отклонения заявки заведующим кафедрой
    /// </summary>
    public DateTime? DepartmentHeadRejectedAt { get; set; }

    /// <summary>
    /// Причина отклонения заявки заведующим кафедрой
    /// </summary>
    public string? DepartmentHeadRejectionReason { get; set; }

    /// <summary>
    /// Дата и время отмены заявки студентом
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual ApplicationStatus Status { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;

    public virtual Topic? Topic { get; set; }
}
