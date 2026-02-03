using System;
using System.Collections.Generic;

namespace DirectoryOfGraduates.Infrastructure.Data.Entities;

/// <summary>
/// Таблица кафедр. Содержит информацию о кафедрах и их заведующих.
/// </summary>
public partial class Department
{
    /// <summary>
    /// Уникальный идентификатор кафедры
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Название кафедры
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Идентификатор заведующего кафедрой (внешний ключ к таблице Users)
    /// </summary>
    public Guid? HeadId { get; set; }

    /// <summary>
    /// Дата и время создания записи о кафедре
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время последнего обновления записи о кафедре
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual User? Head { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
