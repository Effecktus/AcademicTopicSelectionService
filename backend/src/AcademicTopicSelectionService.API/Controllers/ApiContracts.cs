using System.ComponentModel.DataAnnotations;

namespace AcademicTopicSelectionService.API.Controllers;

/// <summary>
/// Тело запроса для создания/полного обновления справочной записи (POST/PUT).
/// Оба поля обязательны.
/// </summary>
/// <param name="CodeName">Системное имя.</param>
/// <param name="DisplayName">Отображаемое имя.</param>
public sealed record UpsertNamedItemRequest(
    [Required] string CodeName,
    [Required] string DisplayName);

/// <summary>
/// Тело запроса для частичного обновления справочной записи (PATCH).
/// Передавайте только поля, которые нужно изменить.
/// </summary>
/// <param name="CodeName">Системное имя (опционально).</param>
/// <param name="DisplayName">Отображаемое имя (опционально).</param>
public sealed record PatchNamedItemRequest(string? CodeName, string? DisplayName);

/// <summary>
/// Тело запроса для создания/полного обновления учёной степени (POST/PUT).
/// CodeName и DisplayName обязательны, ShortName опционален.
/// </summary>
/// <param name="CodeName">Системное имя.</param>
/// <param name="DisplayName">Отображаемое имя.</param>
/// <param name="ShortName">Сокращённое название (опционально).</param>
public sealed record UpsertAcademicDegreeItemRequest(
    [Required] string CodeName,
    [Required] string DisplayName,
    string? ShortName);

/// <summary>
/// Тело запроса для частичного обновления учёной степени (PATCH).
/// Передавайте только поля, которые нужно изменить. ShortName: пустая строка — очистить.
/// </summary>
/// <param name="CodeName">Системное имя (опционально).</param>
/// <param name="DisplayName">Отображаемое имя (опционально).</param>
/// <param name="ShortName">Сокращённое название (опционально).</param>
public sealed record PatchAcademicDegreeItemRequest(string? CodeName, string? DisplayName, string? ShortName);

/// <summary>
/// Тело запроса для создания/полного обновления учебной группы (POST/PUT).
/// CodeName — номер группы в формате XXXX (1000–9999).
/// </summary>
/// <param name="CodeName">Номер учебной группы (1000–9999).</param>
public sealed record UpsertStudyGroupRequest(
    [Required] int CodeName);

/// <summary>
/// Тело запроса для частичного обновления учебной группы (PATCH).
/// Передавайте только поля, которые нужно изменить.
/// </summary>
/// <param name="CodeName">Номер учебной группы (1000–9999), опционально.</param>
public sealed record PatchStudyGroupRequest(int? CodeName);
