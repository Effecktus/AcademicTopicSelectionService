using System;
using System.Collections.Generic;

namespace DirectoryOfGraduates.Infrastructure.Data.Entities;

/// <summary>
/// Справочник ученых степеней. Содержит системные, отображаемые и сокращенные названия степеней.
/// </summary>
public partial class AcademicDegree
{
    /// <summary>
    /// Уникальный идентификатор ученой степени
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Системное значение степени (для кода), регистронезависимо
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Отображаемое значение степени (для пользовательского интерфейса)
    /// </summary>
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// Сокращенное название степени (для отображения в кратких формах)
    /// </summary>
    public string? ShortName { get; set; }

    /// <summary>
    /// Дата и время создания записи о степени
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время последнего обновления записи о степени
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();
}
