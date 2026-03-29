using AcademicTopicSelectionService.Domain.Common;

namespace AcademicTopicSelectionService.Domain.Entities;

/// <summary>
/// Таблица действий по заявкам студентов. Хранит историю согласований:
/// каждое действие описывает один этап рассмотрения заявки (преподавателем или
/// заведующим кафедрой). Новое действие создаётся при переходе заявки на следующий этап.
/// </summary>
public partial class ApplicationAction : IAuditableEntity
{
    /// <summary>
    /// Уникальный идентификатор действия
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор заявки, к которой относится действие (внешний ключ к таблице StudentApplications)
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Идентификатор пользователя, ответственного за данный этап согласования (внешний ключ к таблице Users)
    /// </summary>
    public Guid ResponsibleId { get; set; }

    /// <summary>
    /// Идентификатор статуса действия: На согласовании / Согласовано / Отклонено / Отменено
    /// (внешний ключ к таблице ApplicationActionStatuses)
    /// </summary>
    public Guid StatusId { get; set; }

    /// <summary>
    /// Комментарий ответственного (причина отклонения или произвольный комментарий при согласовании)
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Дата и время создания действия
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время последнего обновления действия
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual StudentApplication Application { get; set; } = null!;

    public virtual User ResponsibleUser { get; set; } = null!;

    public virtual ApplicationActionStatus Status { get; set; } = null!;
}
