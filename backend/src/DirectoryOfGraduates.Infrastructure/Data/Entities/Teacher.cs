using System;
using System.Collections.Generic;

namespace DirectoryOfGraduates.Infrastructure.Data.Entities;

/// <summary>
/// Таблица преподавателей. Содержит дополнительную информацию о преподавателях: академические данные и лимит студентов.
/// </summary>
public partial class Teacher
{
    /// <summary>
    /// Уникальный идентификатор преподавателя
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя-преподавателя (внешний ключ к таблице Users)
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Максимальное количество студентов, которых может взять преподаватель для руководства ВКР
    /// </summary>
    public int? MaxStudentsLimit { get; set; }

    /// <summary>
    /// Идентификатор ученой степени преподавателя (внешний ключ к таблице AcademicDegrees)
    /// </summary>
    public Guid AcademicDegreeId { get; set; }

    /// <summary>
    /// Идентификатор ученого звания преподавателя (внешний ключ к таблице AcademicTitles)
    /// </summary>
    public Guid AcademicTitleId { get; set; }

    /// <summary>
    /// Идентификатор должности преподавателя (внешний ключ к таблице Positions)
    /// </summary>
    public Guid PositionId { get; set; }

    /// <summary>
    /// Дата и время создания записи о преподавателе
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время последнего обновления записи о преподавателе
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual AcademicDegree AcademicDegree { get; set; } = null!;

    public virtual AcademicTitle AcademicTitle { get; set; } = null!;

    public virtual ICollection<GraduateWork> GraduateWorks { get; set; } = new List<GraduateWork>();

    public virtual Position Position { get; set; } = null!;

    public virtual ICollection<Topic> Topics { get; set; } = new List<Topic>();

    public virtual User User { get; set; } = null!;
}
