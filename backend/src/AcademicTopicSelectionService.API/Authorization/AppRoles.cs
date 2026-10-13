namespace AcademicTopicSelectionService.API.Authorization;

/// <summary>
/// Системные имена ролей из справочника <c>UserRoles</c> (см. <c>SystemSeed.UserRoles</c> и <c>ApplicationDbContext.SeedData.cs</c>).
/// Должны совпадать с <c>CodeName</c> в JWT (claim роли, <c>ClaimTypes.Role</c>).
/// </summary>
public static class AppRoles
{
    public const string Student = "Student";
    public const string Teacher = "Teacher";
    public const string DepartmentHead = "DepartmentHead";
    public const string Admin = "Admin";
}
