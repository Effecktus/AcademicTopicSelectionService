namespace AcademicTopicSelectionService.API.Authorization;

/// <summary>
/// Системные имена ролей из справочника <c>UserRoles</c> (см. <c>infra/db/init/01_create_user_roles.sql</c>).
/// Должны совпадать с <c>CodeName</c> в JWT (claim роли, <c>ClaimTypes.Role</c>).
/// </summary>
public static class AppRoles
{
    public const string Student = "Student";
    public const string Teacher = "Teacher";
    public const string DepartmentHead = "DepartmentHead";
    public const string Admin = "Admin";
}
