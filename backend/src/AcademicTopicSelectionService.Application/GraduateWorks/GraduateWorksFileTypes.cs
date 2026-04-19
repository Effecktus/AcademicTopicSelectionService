namespace AcademicTopicSelectionService.Application.GraduateWorks;

/// <summary>
/// Допустимые типы файлов ВКР в API (сегмент URL и ключ объекта в хранилище).
/// </summary>
public static class GraduateWorksFileTypes
{
    public const string Thesis = "thesis";

    public const string Presentation = "presentation";

    public static bool TryNormalize(string? fileType, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(fileType))
            return false;

        var s = fileType.Trim();
        if (s.Equals(Thesis, StringComparison.OrdinalIgnoreCase))
        {
            normalized = Thesis;
            return true;
        }

        if (s.Equals(Presentation, StringComparison.OrdinalIgnoreCase))
        {
            normalized = Presentation;
            return true;
        }

        return false;
    }
}
