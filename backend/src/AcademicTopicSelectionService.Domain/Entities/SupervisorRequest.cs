using AcademicTopicSelectionService.Domain.Common;

namespace AcademicTopicSelectionService.Domain.Entities;

/// <summary>
/// Запрос студента на выбор научного руководителя.
/// </summary>
public partial class SupervisorRequest : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    public Guid TeacherUserId { get; set; }

    public Guid StatusId { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Student Student { get; set; } = null!;

    public virtual User TeacherUser { get; set; } = null!;

    public virtual ApplicationStatus Status { get; set; } = null!;

    public virtual ICollection<StudentApplication> StudentApplications { get; set; } = new List<StudentApplication>();
}
