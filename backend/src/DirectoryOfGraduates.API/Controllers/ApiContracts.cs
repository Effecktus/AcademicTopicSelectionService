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

/// <summary>
/// Тело запроса для создания/полного обновления учёной степени (POST/PUT).
/// Name и DisplayName обязательны, ShortName опционален.
/// </summary>
/// <param name="Name">Системное имя.</param>
/// <param name="DisplayName">Отображаемое имя.</param>
/// <param name="ShortName">Сокращённое название (опционально).</param>
public sealed record UpsertAcademicDegreeItemRequest(
    [Required] string Name,
    [Required] string DisplayName,
    string? ShortName);

/// <summary>
/// Тело запроса для частичного обновления учёной степени (PATCH).
/// Передавайте только поля, которые нужно изменить. ShortName: пустая строка — очистить.
/// </summary>
/// <param name="Name">Системное имя (опционально).</param>
/// <param name="DisplayName">Отображаемое имя (опционально).</param>
/// <param name="ShortName">Сокращённое название (опционально).</param>
public sealed record PatchAcademicDegreeItemRequest(string? Name, string? DisplayName, string? ShortName);
