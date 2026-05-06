using AcademicTopicSelectionService.Domain.Common;

namespace AcademicTopicSelectionService.Domain.Entities;

/// <summary>
/// История изменений названия и описания темы по заявке.
/// </summary>
public partial class ApplicationTopicChangeHistory : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid ApplicationId { get; set; }

    public Guid ChangedByUserId { get; set; }

    /// <summary>
    /// Код типа изменения: TopicTitle или TopicDescription.
    /// </summary>
    public string ChangeKind { get; set; } = null!;

    public string? NewValue { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual StudentApplication Application { get; set; } = null!;

    public virtual User ChangedByUser { get; set; } = null!;
}
