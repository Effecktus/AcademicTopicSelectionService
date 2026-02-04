using System;
using System.Collections.Generic;
using DirectoryOfGraduates.Infrastructure.Data;

namespace DirectoryOfGraduates.Infrastructure.Data.Entities;

/// <summary>
/// Справочник должностей преподавателей. Содержит системные и отображаемые названия должностей.
/// </summary>
public partial class Position : IAuditableEntity
{
    /// <summary>
    /// Уникальный идентификатор должности
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Системное значение должности (для кода), регистронезависимо
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Отображаемое значение должности (для пользовательского интерфейса)
    /// </summary>
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// Дата и время создания записи о должности
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время последнего обновления записи о должности
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();
}
