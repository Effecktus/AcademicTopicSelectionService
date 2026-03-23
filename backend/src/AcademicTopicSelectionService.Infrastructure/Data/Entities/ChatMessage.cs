using System;
using System.Collections.Generic;

namespace AcademicTopicSelectionService.Infrastructure.Data.Entities;

/// <summary>
/// Таблица сообщений чата между студентами и преподавателями по заявкам на темы ВКР. Содержит историю переписки и информацию о прочтении сообщений.
/// </summary>
public partial class ChatMessage
{
    /// <summary>
    /// Уникальный идентификатор сообщения
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор заявки, к которой относится сообщение (внешний ключ к таблице Applications)
    /// </summary>
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Идентификатор отправителя сообщения (внешний ключ к таблице Users)
    /// </summary>
    public Guid SenderId { get; set; }

    /// <summary>
    /// Текст сообщения
    /// </summary>
    public string Content { get; set; } = null!;

    /// <summary>
    /// Дата и время отправки сообщения
    /// </summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// Дата и время прочтения сообщения получателем (NULL, если сообщение не прочитано)
    /// </summary>
    public DateTime? ReadAt { get; set; }

    public virtual StudentApplication Application { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}
