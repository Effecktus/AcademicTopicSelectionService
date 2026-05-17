using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Admin;
using AcademicTopicSelectionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Repositories;

/// <summary>
/// Реализация агрегирующих запросов для панели администратора.
/// </summary>
public sealed class AdminRepository(ApplicationDbContext db) : IAdminRepository
{
    /// <inheritdoc />
    public async Task<AdminAnalyticsDto> GetAnalyticsAsync(CancellationToken ct)
    {
        var byStatus = await db.StudentApplications
            .AsNoTracking()
            .GroupBy(a => new { a.Status.CodeName, a.Status.DisplayName })
            .Select(g => new StatusCountDto(g.Key.CodeName, g.Key.DisplayName, g.LongCount()))
            .OrderBy(x => x.StatusCode)
            .ToListAsync(ct);

        var byYear = await db.GraduateWorks
            .AsNoTracking()
            .GroupBy(gw => gw.Year)
            .Select(g => new YearCountDto(g.Key, g.LongCount()))
            .OrderByDescending(x => x.Year)
            .ToListAsync(ct);

        var byDepartment = await db.StudentApplications
            .AsNoTracking()
            .Where(a => a.SupervisorRequest != null
                        && a.SupervisorRequest.TeacherUser.DepartmentId != null)
            .GroupBy(a => a.SupervisorRequest!.TeacherUser.Department!.DisplayName)
            .Select(g => new DepartmentCountDto(g.Key, g.LongCount()))
            .OrderBy(x => x.DepartmentName)
            .ToListAsync(ct);

        return new AdminAnalyticsDto(byStatus, byYear, byDepartment);
    }

    /// <inheritdoc />
    public Task<List<GwExportRow>> GetGwExportAsync(CancellationToken ct)
        => db.GraduateWorks
            .AsNoTracking()
            .OrderByDescending(gw => gw.Year).ThenBy(gw => gw.Student.User.LastName)
            .Select(gw => new GwExportRow(
                gw.Title,
                gw.Student.User.LastName + " " + gw.Student.User.FirstName
                    + (gw.Student.User.MiddleName != null ? " " + gw.Student.User.MiddleName : ""),
                gw.Teacher.User.LastName + " " + gw.Teacher.User.FirstName
                    + (gw.Teacher.User.MiddleName != null ? " " + gw.Teacher.User.MiddleName : ""),
                gw.Year,
                gw.Grade,
                gw.CommissionMembers,
                gw.FilePath != null,
                gw.PresentationPath != null,
                gw.CreatedAt))
            .ToListAsync(ct);

    /// <inheritdoc />
    public Task<List<ApplicationExportRow>> GetApplicationsExportAsync(CancellationToken ct)
        => db.StudentApplications
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ApplicationExportRow(
                a.Topic.Title,
                a.Student.User.LastName + " " + a.Student.User.FirstName
                    + (a.Student.User.MiddleName != null ? " " + a.Student.User.MiddleName : ""),
                a.Student.Group.CodeName.ToString(),
                a.SupervisorRequest != null
                    ? a.SupervisorRequest.TeacherUser.LastName + " " + a.SupervisorRequest.TeacherUser.FirstName
                    : "",
                a.Status.DisplayName,
                a.CreatedAt))
            .ToListAsync(ct);

    /// <inheritdoc />
    public Task<List<UserExportRow>> GetUsersExportAsync(CancellationToken ct)
        => db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Department)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Select(u => new UserExportRow(
                u.Email,
                u.LastName,
                u.FirstName,
                u.MiddleName,
                u.Role.DisplayName,
                u.Department != null ? u.Department.DisplayName : null,
                u.IsActive,
                u.CreatedAt))
            .ToListAsync(ct);
}
