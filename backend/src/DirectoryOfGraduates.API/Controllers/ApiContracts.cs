using System.ComponentModel.DataAnnotations;

namespace DirectoryOfGraduates.API.Controllers;

/// <summary>
/// Тело запроса для создания/полного обновления справочной записи (POST/PUT).
/// Оба поля обязательны.
/// </summary>
/// <param name="Name">Системное имя.</param>
/// <param name="DisplayName">Отображаемое имя.</param>
public sealed record UpsertNamedItemRequest(
    [Required] string Name,
    [Required] string DisplayName);

/// <summary>
/// Тело запроса для частичного обновления справочной записи (PATCH).
/// Передавайте только поля, которые нужно изменить.
/// </summary>
/// <param name="Name">Системное имя (опционально).</param>
/// <param name="DisplayName">Отображаемое имя (опционально).</param>
public sealed record PatchNamedItemRequest(string? Name, string? DisplayName);
