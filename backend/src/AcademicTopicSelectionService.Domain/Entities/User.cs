using AcademicTopicSelectionService.Domain.Common;

namespace AcademicTopicSelectionService.Domain.Entities;

/// <summary>
/// Таблица пользователей системы. Содержит основную информацию о пользователях: учетные данные, персональные данные, роль и принадлежность к кафедре.
/// </summary>
public partial class User : IAuditableEntity
{
    /// <summary>
    /// Уникальный идентификатор пользователя
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Email пользователя (уникальный, регистронезависимый)
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// Хеш пароля пользователя
    /// </summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>
    /// Имя пользователя
    /// </summary>
    public string FirstName { get; set; } = null!;

    /// <summary>
    /// Фамилия пользователя
    /// </summary>
    public string LastName { get; set; } = null!;

    /// <summary>
    /// Отчество пользователя
    /// </summary>
    public string? MiddleName { get; set; }

    /// <summary>
    /// Идентификатор роли пользователя (внешний ключ к таблице UserRoles)
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Идентификатор кафедры пользователя (внешний ключ к таблице Departments)
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Дата и время создания записи о пользователе
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время последнего обновления записи о пользователе
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Флаг активности пользователя (true - активен, false - деактивирован)
    /// </summary>
    public bool IsActive { get; set; }

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual Department? Department { get; set; }

    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual UserRole Role { get; set; } = null!;

    public virtual Student? Student { get; set; }

    public virtual Teacher? Teacher { get; set; }
}
