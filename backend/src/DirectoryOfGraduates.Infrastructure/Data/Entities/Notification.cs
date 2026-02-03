using System;
using System.Collections.Generic;

namespace DirectoryOfGraduates.Infrastructure.Data.Entities;

/// <summary>
/// Таблица уведомлений пользователей системы. Содержит информацию о различных типах уведомлений и их статусе прочтения.
/// </summary>
public partial class Notification
{
    /// <summary>
    /// Уникальный идентификатор уведомления
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя-получателя уведомления (внешний ключ к таблице Users)
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Идентификатор типа уведомления (внешний ключ к таблице NotificationTypes)
    /// </summary>
    public Guid TypeId { get; set; }

    /// <summary>
    /// Заголовок уведомления
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// Содержимое уведомления (полный текст)
    /// </summary>
    public string Content { get; set; } = null!;

    /// <summary>
    /// Флаг прочтения уведомления (true - прочитано, false - не прочитано)
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Дата и время создания уведомления
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public virtual NotificationType Type { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
