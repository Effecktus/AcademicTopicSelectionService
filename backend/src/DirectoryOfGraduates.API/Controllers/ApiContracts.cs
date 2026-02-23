using System.ComponentModel.DataAnnotations;

namespace DirectoryOfGraduates.API.Controllers;

/// <summary>
/// Обёртка для постраничного ответа API.
/// </summary>
/// <typeparam name="T">Тип элементов списка.</typeparam>
/// <param name="Page">Текущий номер страницы.</param>
/// <param name="PageSize">Количество элементов на странице.</param>
/// <param name="Total">Общее количество элементов.</param>
/// <param name="Items">Элементы текущей страницы.</param>
public sealed record ListResponse<T>(int Page, int PageSize, long Total, IReadOnlyList<T> Items);

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
