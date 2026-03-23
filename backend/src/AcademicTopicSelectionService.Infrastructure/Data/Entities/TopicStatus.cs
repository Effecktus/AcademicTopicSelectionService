using System;
using System.Collections.Generic;
using AcademicTopicSelectionService.Infrastructure.Data;

namespace AcademicTopicSelectionService.Infrastructure.Data.Entities;

/// <summary>
/// Справочник статусов тем ВКР. Содержит системные и отображаемые названия статусов.
/// </summary>
public partial class TopicStatus : IAuditableEntity
{
    /// <summary>
    /// Уникальный идентификатор статуса темы
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Системное значение статуса (для кода), регистронезависимо
    /// </summary>
    public string Name { get; set; } = null!;

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

    public virtual ICollection<Topic> Topics { get; set; } = new List<Topic>();
}
