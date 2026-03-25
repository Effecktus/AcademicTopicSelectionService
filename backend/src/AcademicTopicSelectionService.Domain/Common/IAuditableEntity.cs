namespace AcademicTopicSelectionService.Domain.Common;

/// <summary>
/// Интерфейс для сущностей с автоматическим аудитом дат создания и обновления.
/// CreatedAt устанавливается при добавлении, UpdatedAt — при изменении.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// Дата и время создания записи (UTC).
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время последнего обновления записи (UTC).
    /// </summary>
    DateTime? UpdatedAt { get; set; }
}
