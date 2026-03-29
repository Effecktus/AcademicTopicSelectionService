using AcademicTopicSelectionService.Domain.Common;

namespace AcademicTopicSelectionService.Domain.Entities;

/// <summary>
/// Таблица заявок студентов на темы ВКР. Содержит информацию о заявках: выбранные темы
/// и текущий статус. История согласований хранится в таблице ApplicationActions.
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
    /// Идентификатор темы ВКР, на которую подана заявка (внешний ключ к таблице Topics)
    /// </summary>
    public Guid TopicId { get; set; }

    /// <summary>
    /// Идентификатор текущего статуса заявки (внешний ключ к таблице ApplicationStatuses),
    /// синхронизируется с последним действием в ApplicationActions
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

    public virtual ICollection<ApplicationAction> ApplicationActions { get; set; } = new List<ApplicationAction>();

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual ApplicationStatus Status { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;

    public virtual Topic Topic { get; set; } = null!;
}
