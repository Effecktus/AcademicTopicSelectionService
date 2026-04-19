using System.Text.RegularExpressions;

namespace AcademicTopicSelectionService.Application.Security;

/// <summary>
/// Правила для email и пароля при создании учётной записи и нормализация email при входе.
/// </summary>
public static class CredentialValidation
{
    private const int EmailMaxLength = 320;
    private const int PasswordMinLength = 10;
    private const int PasswordMaxLength = 128;

    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(250));

    /// <summary>
    /// Проверяет и нормализует email (trim, нижний регистр через ToLowerInvariant).
    /// </summary>
    /// <returns><c>true</c>, если email допустим; иначе <paramref name="error"/> заполняется.</returns>
    public static bool TryNormalizeEmail(string? email, out string normalized, out string? error)
    {
        normalized = "";
        error = null;

        if (string.IsNullOrWhiteSpace(email))
        {
            error = "Email is required";
            return false;
        }

        var trimmed = email.Trim();
        if (trimmed.Length > EmailMaxLength)
        {
            error = "Email is too long";
            return false;
        }

        normalized = trimmed.ToLowerInvariant();
        if (!EmailRegex.IsMatch(normalized))
        {
            error = "Email is invalid";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Проверка пароля при создании пользователя администратором.
    /// </summary>
    public static string? ValidatePasswordForNewUser(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return "Password is required";
        if (password.Length < PasswordMinLength)
            return $"Password must be at least {PasswordMinLength} characters";
        if (password.Length > PasswordMaxLength)
            return $"Password must be at most {PasswordMaxLength} characters";
        if (password.Any(char.IsWhiteSpace))
            return "Password must not contain whitespace";

        var hasLetter = false;
        var hasDigit = false;
        foreach (var c in password)
        {
            if (char.IsLetter(c))
                hasLetter = true;
            else if (char.IsDigit(c))
                hasDigit = true;
            if (hasLetter && hasDigit)
                break;
        }

        if (!hasLetter || !hasDigit)
            return "Password must contain at least one letter and one digit";

        return null;
    }
}
