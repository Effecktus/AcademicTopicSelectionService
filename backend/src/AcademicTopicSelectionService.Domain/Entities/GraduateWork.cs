using AcademicTopicSelectionService.Domain.Common;

namespace AcademicTopicSelectionService.Domain.Entities;

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
    /// Идентификатор заявки студента (внешний ключ к таблице StudentApplications); одна ВКР на заявку.
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Идентификатор статуса ВКР (внешний ключ к таблице GraduateWorkStatuses)
    /// </summary>
    public Guid StatusId { get; set; }

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
    /// Оценка за работу (от 0 до 100 баллов); null до заполнения администратором после защиты
    /// </summary>
    public int? Grade { get; set; }

    /// <summary>
    /// Состав комиссии, оценивавшей работу (текстовое описание); null до заполнения администратором после защиты
    /// </summary>
    public string? CommissionMembers { get; set; }

    /// <summary>
    /// Ключ объекта основного файла ВКР в объектном хранилище; null до подтверждения загрузки.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Оригинальное имя файла ВКР с расширением; используется в Content-Disposition при скачивании.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Путь к файлу презентации работы (опционально)
    /// </summary>
    public string? PresentationPath { get; set; }

    /// <summary>
    /// Оригинальное имя файла презентации с расширением; используется в Content-Disposition при скачивании.
    /// </summary>
    public string? PresentationFileName { get; set; }

    /// <summary>
    /// Дата и время создания записи о работе
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время последнего обновления записи о работе
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    public virtual StudentApplication Application { get; set; } = null!;

    public virtual GraduateWorkStatus Status { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;

    public virtual Teacher Teacher { get; set; } = null!;
}
