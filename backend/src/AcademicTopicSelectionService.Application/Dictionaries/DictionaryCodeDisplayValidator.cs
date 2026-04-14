namespace AcademicTopicSelectionService.Application.Dictionaries;

/// <summary>
/// Общая валидация пары CodeName + DisplayName для справочников (те же правила и сообщения, что в сервисах до рефакторинга).
/// </summary>
internal static class DictionaryCodeDisplayValidator
{
    public const int DefaultMaxDisplayNameLength = 100;

    public static (bool ok, string codeName, string displayName, string error) Validate(
        string? codeNameRaw,
        string? displayNameRaw,
        int maxDisplayNameLength = DefaultMaxDisplayNameLength)
    {
        var codeName = (codeNameRaw ?? string.Empty).Trim();
        var displayName = (displayNameRaw ?? string.Empty).Trim();

        if (codeName.Length == 0)
            return (false, string.Empty, string.Empty, "CodeName is required.");

        if (displayName.Length == 0)
            return (false, string.Empty, string.Empty, "DisplayName is required");
        if (displayName.Length > maxDisplayNameLength)
            return (false, string.Empty, string.Empty, $"DisplayName must be <= {maxDisplayNameLength} chars");

        return (true, codeName, displayName, string.Empty);
    }

    public static (bool ok, string? codeName, string? displayName, string error) ValidatePatch(
        string? codeNameRaw,
        string? displayNameRaw,
        int maxDisplayNameLength = DefaultMaxDisplayNameLength)
    {
        string? codeName = null;
        string? displayName = null;

        if (codeNameRaw is not null)
        {
            codeName = codeNameRaw.Trim();
            if (codeName.Length == 0)
                return (false, null, null, "CodeName cannot be empty if provided.");
        }

        if (displayNameRaw is not null)
        {
            displayName = displayNameRaw.Trim();
            if (displayName.Length == 0)
                return (false, null, null, "DisplayName cannot be empty if provided");
            if (displayName.Length > maxDisplayNameLength)
                return (false, null, null, $"DisplayName must be <= {maxDisplayNameLength} chars");
        }

        if (codeName is null && displayName is null)
            return (false, null, null, "At least one field must be provided");

        return (true, codeName, displayName, string.Empty);
    }

    /// <summary>
    /// Полная валидация для записей с опциональным ShortName (степени).
    /// </summary>
    public static (bool ok, string codeName, string displayName, string? shortName, string error) ValidateWithOptionalShortName(
        string? codeNameRaw,
        string? displayNameRaw,
        string? shortNameRaw,
        int maxDisplayNameLength = DefaultMaxDisplayNameLength,
        int maxShortNameLength = 50)
    {
        var (ok, codeName, displayName, error) = Validate(codeNameRaw, displayNameRaw, maxDisplayNameLength);
        if (!ok)
            return (false, string.Empty, string.Empty, null, error);

        var shortName = string.IsNullOrWhiteSpace(shortNameRaw) ? null : shortNameRaw.Trim();
        if (shortName is not null && shortName.Length > maxShortNameLength)
            return (false, string.Empty, string.Empty, null, $"ShortName must be <= {maxShortNameLength} chars");

        return (true, codeName, displayName, shortName, string.Empty);
    }

    /// <summary>
    /// PATCH для степеней: три опциональных поля, хотя бы одно должно быть передано.
    /// </summary>
    public static (bool ok, string? codeName, string? displayName, string? shortName, string error) ValidatePatchWithOptionalShortName(
        string? codeNameRaw,
        string? displayNameRaw,
        string? shortNameInPatch,
        int maxDisplayNameLength = DefaultMaxDisplayNameLength,
        int maxShortNameLength = 50)
    {
        string? codeName = null;
        string? displayName = null;
        string? shortName = null;

        if (codeNameRaw is not null)
        {
            codeName = codeNameRaw.Trim();
            if (codeName.Length == 0)
                return (false, null, null, null, "CodeName cannot be empty if provided.");
        }

        if (displayNameRaw is not null)
        {
            displayName = displayNameRaw.Trim();
            if (displayName.Length == 0)
                return (false, null, null, null, "DisplayName cannot be empty if provided");
            if (displayName.Length > maxDisplayNameLength)
                return (false, null, null, null, $"DisplayName must be <= {maxDisplayNameLength} chars");
        }

        if (shortNameInPatch is not null)
        {
            shortName = string.IsNullOrWhiteSpace(shortNameInPatch) ? "" : shortNameInPatch.Trim();
            if (shortName.Length > maxShortNameLength)
                return (false, null, null, null, $"ShortName must be <= {maxShortNameLength} chars");
        }

        if (codeName is null && displayName is null && shortNameInPatch is null)
            return (false, null, null, null, "At least one field must be provided");

        return (true, codeName, displayName, shortName, string.Empty);
    }
}
