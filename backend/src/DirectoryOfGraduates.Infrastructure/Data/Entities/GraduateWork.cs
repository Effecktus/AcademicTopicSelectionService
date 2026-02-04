using System;
using System.Collections.Generic;
using DirectoryOfGraduates.Infrastructure.Data;

namespace DirectoryOfGraduates.Infrastructure.Data.Entities;

/// <summary>
/// Таблица выпускных квалификационных работ (ВКР). Содержит информацию о завершенных работах студентов: название, оценки, файлы работ и презентаций, состав комиссии.
/// </summary>
public partial class GraduateWork : IAuditableEntity
{
    /// <summary>
    /// Уникальный идентификатор выпускной квалификационной работы
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Название выпускной квалификационной работы (регистронезависимо)
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// Идентификатор студента, выполнившего работу (внешний ключ к таблице Students)
    /// </summary>
    public Guid StudentId { get; set; }

    /// <summary>
    /// Идентификатор преподавателя-руководителя работы (внешний ключ к таблице Teachers)
    /// </summary>
    public Guid TeacherId { get; set; }

    /// <summary>
    /// Учебный год, в котором была выполнена работа
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Оценка за работу (от 0 до 100 баллов)
    /// </summary>
    public int Grade { get; set; }

    /// <summary>
    /// Состав комиссии, оценивавшей работу (текстовое описание)
    /// </summary>
    public string CommissionMembers { get; set; } = null!;

    /// <summary>
    /// Путь к файлу выпускной квалификационной работы (не может быть пустым)
    /// </summary>
    public string FilePath { get; set; } = null!;

    /// <summary>
    /// Путь к файлу презентации работы (опционально)
    /// </summary>
    public string? PresentationPath { get; set; }

    /// <summary>
    /// Дата и время создания записи о работе
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время последнего обновления записи о работе
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual Student Student { get; set; } = null!;

    public virtual Teacher Teacher { get; set; } = null!;
}
