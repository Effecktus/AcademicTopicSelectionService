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
    public async Task<AdminAnalyticsDto> GetAnalyticsAsync(int? year, CancellationToken ct)
    {
        var currentYear = DateTime.UtcNow.Year;
        var monthsYear = year ?? currentYear;

        var totalApplications = await db.StudentApplications
            .Where(a => year == null || a.CreatedAt.Year == year)
            .LongCountAsync(ct);
        var totalGraduateWorks = await db.GraduateWorks
            .Where(gw => year == null || gw.Year == year)
            .LongCountAsync(ct);
        var totalUsers = await db.Users
            .Where(u => year == null || u.CreatedAt.Year == year)
            .LongCountAsync(ct);
        var summary = new AdminSummaryDto(totalApplications, totalGraduateWorks, totalUsers);

        var statusRaw = await db.StudentApplications
            .AsNoTracking()
            .Where(a => year == null || a.CreatedAt.Year == year)
            .Select(a => new { a.Status.CodeName, a.Status.DisplayName })
            .ToListAsync(ct);
        var byStatus = statusRaw
            .GroupBy(a => new { a.CodeName, a.DisplayName })
            .Select(g => new StatusCountDto(g.Key.CodeName, g.Key.DisplayName, g.LongCount()))
            .OrderBy(x => x.StatusCode)
            .ToList();

        var yearRaw = await db.GraduateWorks
            .AsNoTracking()
            .Where(gw => year == null || gw.Year == year)
            .Select(gw => gw.Year)
            .ToListAsync(ct);
        var byYear = yearRaw
            .GroupBy(y => y)
            .Select(g => new YearCountDto(g.Key, g.LongCount()))
            .OrderByDescending(x => x.Year)
            .ToList();

        var deptRaw = await db.StudentApplications
            .AsNoTracking()
            .Where(a => (year == null || a.CreatedAt.Year == year)
                        && a.SupervisorRequest != null
                        && a.SupervisorRequest.TeacherUser.DepartmentId != null)
            .Select(a => a.SupervisorRequest!.TeacherUser.Department!.DisplayName)
            .ToListAsync(ct);
        var byDepartment = deptRaw
            .GroupBy(name => name)
            .Select(g => new DepartmentCountDto(g.Key, g.LongCount()))
            .OrderBy(x => x.DepartmentName)
            .ToList();

        var monthRaw = await db.StudentApplications
            .AsNoTracking()
            .Where(a => a.CreatedAt.Year == monthsYear)
            .Select(a => a.CreatedAt.Month)
            .ToListAsync(ct);
        var byMonth = monthRaw
            .GroupBy(m => m)
            .Select(g => new MonthCountDto(g.Key, g.LongCount()))
            .OrderBy(x => x.Month)
            .ToList();

        var teacherRaw = await db.StudentApplications
            .AsNoTracking()
            .Where(a => (year == null || a.CreatedAt.Year == year)
                        && a.SupervisorRequest != null)
            .Select(a => new
            {
                Id = a.SupervisorRequest!.TeacherUser.Id,
                FirstName = a.SupervisorRequest!.TeacherUser.FirstName,
                LastName = a.SupervisorRequest!.TeacherUser.LastName,
                MiddleName = a.SupervisorRequest!.TeacherUser.MiddleName,
                DeptName = a.SupervisorRequest!.TeacherUser.Department != null
                    ? a.SupervisorRequest!.TeacherUser.Department.DisplayName
                    : null
            })
            .ToListAsync(ct);

        var topTeachers = teacherRaw
            .GroupBy(t => t.Id)
            .Select(g =>
            {
                var first = g.First();
                var fullName = first.LastName + " " + first.FirstName +
                               (string.IsNullOrEmpty(first.MiddleName) ? "" : " " + first.MiddleName);
                return new TeacherCountDto(fullName, first.DeptName, g.LongCount());
            })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        return new AdminAnalyticsDto(summary, byStatus, byYear, byDepartment, byMonth, topTeachers);
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
