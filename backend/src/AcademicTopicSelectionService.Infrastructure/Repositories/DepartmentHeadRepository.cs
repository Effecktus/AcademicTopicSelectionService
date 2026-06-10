using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.DepartmentHead;
using AcademicTopicSelectionService.Application.StudentApplications;
using AcademicTopicSelectionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Repositories;

/// <summary>
/// Реализация агрегирующих запросов для страницы аналитики заведующего кафедрой.
/// </summary>
public sealed class DepartmentHeadRepository(ApplicationDbContext db) : IDepartmentHeadRepository
{
    /// <inheritdoc />
    public async Task<DepartmentHeadAnalyticsDto?> GetAnalyticsAsync(
        Guid departmentHeadUserId, int? year, CancellationToken ct)
    {
        var dhUser = await db.Users.AsNoTracking()
            .Where(u => u.Id == departmentHeadUserId)
            .Select(u => new { u.DepartmentId })
            .FirstOrDefaultAsync(ct);

        if (dhUser?.DepartmentId is not { } deptId)
            return null;

        var totalTopics = await db.Topics.AsNoTracking()
            .LongCountAsync(t => t.CreatedByUser.DepartmentId == deptId
                                 && (year == null || t.CreatedAt.Year == year), ct);

        var totalStudents = await db.Students.AsNoTracking()
            .LongCountAsync(s => s.User.DepartmentId == deptId
                                 && (year == null || s.User.CreatedAt.Year == year), ct);

        var totalApplications = await db.StudentApplications.AsNoTracking()
            .LongCountAsync(a => a.SupervisorRequest != null
                                 && a.SupervisorRequest.TeacherUser.DepartmentId == deptId
                                 && (year == null || a.CreatedAt.Year == year), ct);

        var totalGraduateWorks = await db.GraduateWorks.AsNoTracking()
            .LongCountAsync(gw => gw.Teacher.User.DepartmentId == deptId
                                  && (year == null || gw.Year == year), ct);

        var summary = new DhSummaryDto(totalTopics, totalStudents, totalApplications, totalGraduateWorks);

        var statusRaw = await db.StudentApplications.AsNoTracking()
            .Where(a => a.SupervisorRequest != null
                        && a.SupervisorRequest.TeacherUser.DepartmentId == deptId
                        && (year == null || a.CreatedAt.Year == year))
            .Select(a => new { a.Status.CodeName, a.Status.DisplayName })
            .ToListAsync(ct);
        var byStatus = statusRaw
            .GroupBy(a => new { a.CodeName, a.DisplayName })
            .Select(g => new DhStatusCountDto(g.Key.CodeName, g.Key.DisplayName, g.LongCount()))
            .OrderBy(x => x.StatusCode)
            .ToList();

        var yearRaw = await db.GraduateWorks.AsNoTracking()
            .Where(gw => gw.Teacher.User.DepartmentId == deptId
                         && (year == null || gw.Year == year))
            .Select(gw => gw.Year)
            .ToListAsync(ct);
        var byYear = yearRaw
            .GroupBy(y => y)
            .Select(g => new DhYearCountDto(g.Key, g.LongCount()))
            .OrderByDescending(x => x.Year)
            .ToList();

        var teacherWorkloadRaw = await db.Teachers.AsNoTracking()
            .Where(t => t.User.DepartmentId == deptId && t.User.IsActive)
            .Select(t => new
            {
                t.User.LastName,
                t.User.FirstName,
                t.User.MiddleName,
                t.MaxStudentsLimit,
                ActiveCount = db.StudentApplications.Count(a =>
                    a.Status.CodeName == ApplicationStatusCodes.ApprovedByDepartmentHead
                    && a.SupervisorRequest != null
                    && a.SupervisorRequest.TeacherUserId == t.UserId
                    && (year == null || a.CreatedAt.Year == year)),
            })
            .ToListAsync(ct);

        var teacherWorkload = teacherWorkloadRaw
            .Select(t =>
            {
                var fullName = string.Join(" ",
                    new[] { t.LastName, t.FirstName, t.MiddleName }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));
                return new DhTeacherWorkloadDto(fullName, t.ActiveCount, t.MaxStudentsLimit);
            })
            .OrderByDescending(t => t.ActiveStudentsCount)
            .ThenBy(t => t.TeacherFullName)
            .ToList();

        return new DepartmentHeadAnalyticsDto(summary, byStatus, byYear, teacherWorkload);
    }
}
