namespace AcademicTopicSelectionService.Infrastructure.Repositories;

/// <summary>
/// Шаблоны для <c>EF.Functions.Like</c>, одинаково работающие в PostgreSQL и SQL Server.
/// </summary>
/// <remarks>
/// Регистронезависимость обеспечивает тип столбца (<c>citext</c> / CI-collation), а не <c>ILIKE</c>,
/// поэтому один и тот же запрос подходит обоим провайдерам.
/// </remarks>
internal static class LikePattern
{
    /// <summary>Символ экранирования для третьего аргумента <c>EF.Functions.Like</c>.</summary>
    public const string EscapeCharacter = "\\";

    /// <summary>
    /// Шаблон «содержит подстроку». Спецсимволы LIKE во вводе пользователя экранируются:
    /// <c>%</c> и <c>_</c> — в обеих СУБД, <c>[</c> — в SQL Server.
    /// </summary>
    public static string Contains(string term) => $"%{Escape(term)}%";

    private static string Escape(string value) => value
        .Replace(EscapeCharacter, EscapeCharacter + EscapeCharacter)
        .Replace("%", EscapeCharacter + "%")
        .Replace("_", EscapeCharacter + "_")
        .Replace("[", EscapeCharacter + "[");
}
