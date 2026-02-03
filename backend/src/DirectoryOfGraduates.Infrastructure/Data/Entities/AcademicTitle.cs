using System;
using System.Collections.Generic;

namespace DirectoryOfGraduates.Infrastructure.Data.Entities;

/// <summary>
/// Справочник ученых званий. Содержит системные и отображаемые названия званий.
/// </summary>
public partial class AcademicTitle
{
    /// <summary>
    /// Уникальный идентификатор ученого звания
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Системное значение звания (для кода), регистронезависимо
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Отображаемое значение звания (для пользовательского интерфейса)
    /// </summary>
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// Дата и время создания записи о звании
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время последнего обновления записи о звании
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();
}
