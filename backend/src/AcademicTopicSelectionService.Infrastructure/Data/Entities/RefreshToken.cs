using System;
using System.Collections.Generic;

namespace AcademicTopicSelectionService.Infrastructure.Data.Entities;

/// <summary>
/// Таблица refresh токенов для JWT аутентификации. Содержит информацию о токенах обновления доступа пользователей, их сроке действия и статусе отзыва.
/// </summary>
public partial class RefreshToken
{
    /// <summary>
    /// Уникальный идентификатор refresh токена
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя, которому принадлежит токен (внешний ключ к таблице Users)
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Значение refresh токена (уникальное)
    /// </summary>
    public string Token { get; set; } = null!;

    /// <summary>
    /// Дата и время истечения срока действия токена
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Дата и время создания токена
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Флаг отзыва токена (true - отозван, false - активен)
    /// </summary>
    public bool IsRevoked { get; set; }

    public virtual User User { get; set; } = null!;
}
