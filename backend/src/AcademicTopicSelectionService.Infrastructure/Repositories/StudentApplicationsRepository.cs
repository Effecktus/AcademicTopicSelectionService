using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.StudentApplications;
using AcademicTopicSelectionService.Domain.Entities;
using AcademicTopicSelectionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория заявок студентов.
/// </summary>
public sealed class StudentApplicationsRepository(ApplicationDbContext db) : IStudentApplicationsRepository
{
    private static readonly HashSet<string> TerminalStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "RejectedBySupervisor",
        "RejectedByDepartmentHead",
        "Cancelled",
        "ApprovedByDepartmentHead"
    };

    /// <inheritdoc />
    public async Task<PagedResult<StudentApplicationDto>> ListForRoleAsync(
        ListApplicationsQuery query, string roleCodeName, Guid userId, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var baseQuery = BuildBaseQuery();

        // Фильтрация по роли
        baseQuery = roleCodeName switch
        {
            "Student" => baseQuery.Where(a => a.Student.UserId == userId),
            "Teacher" => baseQuery.Where(a => a.SupervisorRequest != null && a.SupervisorRequest.TeacherUserId == userId),
            "DepartmentHead" => baseQuery.Where(a =>
                a.SupervisorRequest != null &&
                a.SupervisorRequest.TeacherUser.DepartmentId != null &&
                a.SupervisorRequest.TeacherUser.Department != null &&
                a.SupervisorRequest.TeacherUser.Department.HeadId == userId),
            "Admin" => baseQuery,
            _ => baseQuery.Where(a => false)
        };

        var totalCount = await baseQuery.LongCountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new StudentApplicationDto(
                a.Id,
                a.StudentId,
                a.Student.User.FirstName,
                a.Student.User.LastName,
                a.Student.Group.CodeName.ToString(),
                a.TopicId,
                a.Topic.Title,
                a.SupervisorRequestId ?? Guid.Empty,
                a.SupervisorRequest == null ? Guid.Empty : a.SupervisorRequest.TeacherUserId,
                a.SupervisorRequest == null ? string.Empty : a.SupervisorRequest.TeacherUser.FirstName,
                a.SupervisorRequest == null ? string.Empty : a.SupervisorRequest.TeacherUser.LastName,
                a.Topic.CreatedBy,
                a.Topic.CreatedByUser.Email,
                a.Topic.CreatedByUser.FirstName,
                a.Topic.CreatedByUser.LastName,
                new ApplicationStatusRefDto(a.Status.Id, a.Status.CodeName, a.Status.DisplayName),
                a.CreatedAt,
                a.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<StudentApplicationDto>(page, pageSize, totalCount, items);

        IQueryable<StudentApplication> BuildBaseQuery() => db.StudentApplications
            .AsNoTracking()
            .Include(a => a.Student).ThenInclude(s => s.User)
            .Include(a => a.Student).ThenInclude(s => s.Group)
            .Include(a => a.SupervisorRequest!).ThenInclude(r => r.TeacherUser).ThenInclude(u => u.Department)
            .Include(a => a.Topic).ThenInclude(t => t.CreatedByUser)
            .Include(a => a.Status);
    }

    /// <inheritdoc />
    public async Task<StudentApplicationDetailDto?> GetDetailAsync(Guid id, CancellationToken ct)
    {
        var app = await db.StudentApplications.AsNoTracking()
            .Include(a => a.Student).ThenInclude(s => s.User)
            .Include(a => a.Student).ThenInclude(s => s.Group)
            .Include(a => a.SupervisorRequest!).ThenInclude(r => r.TeacherUser)
            .Include(a => a.Topic).ThenInclude(t => t.CreatedByUser)
            .Include(a => a.Status)
            .Include(a => a.ApplicationActions)
                .ThenInclude(aa => aa.Status)
            .Include(a => a.ApplicationActions)
                .ThenInclude(aa => aa.ResponsibleUser)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (app is null) return null;

        var actions = app.ApplicationActions
            .OrderBy(aa => aa.CreatedAt)
            .Select(aa => new ApplicationActionSnapshotDto(
                aa.Id,
                aa.ResponsibleId,
                aa.ResponsibleUser.FirstName,
                aa.ResponsibleUser.LastName,
                aa.Status.CodeName,
                aa.Status.DisplayName,
                aa.Comment,
                aa.CreatedAt))
            .ToList();

        return new StudentApplicationDetailDto(
            app.Id,
            app.StudentId,
            app.Student.User.FirstName,
            app.Student.User.LastName,
            app.Student.Group.CodeName.ToString(),
            app.TopicId,
            app.Topic.Title,
            app.Topic.Description,
            app.SupervisorRequestId,
            app.SupervisorRequest?.TeacherUserId ?? Guid.Empty,
            app.SupervisorRequest?.TeacherUser.FirstName ?? string.Empty,
            app.SupervisorRequest?.TeacherUser.LastName ?? string.Empty,
            app.SupervisorRequest?.TeacherUser.DepartmentId,
            app.Topic.CreatedBy,
            app.Topic.CreatedByUser.FirstName,
            app.Topic.CreatedByUser.LastName,
            app.Topic.CreatedByUser.DepartmentId,
            new ApplicationStatusRefDto(app.Status.Id, app.Status.CodeName, app.Status.DisplayName),
            app.CreatedAt,
            app.UpdatedAt,
            actions);
    }

    /// <inheritdoc />
    public async Task<StudentApplication?> GetByIdWithTrackingAsync(Guid id, CancellationToken ct)
    {
        return await db.StudentApplications
            .Include(a => a.Student)
                .ThenInclude(s => s.User)
            .Include(a => a.Topic)
            .Include(a => a.SupervisorRequest)
            .Include(a => a.Status)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task<StudentApplication> AddAsync(StudentApplication application, CancellationToken ct)
    {
        db.StudentApplications.Add(application);
        await db.SaveChangesAsync(ct);
        return application;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> HasActiveApplicationOnTopicAsync(Guid topicId, CancellationToken ct)
    {
        return await db.StudentApplications.AsNoTracking().AnyAsync(a =>
            a.TopicId == topicId && !TerminalStatuses.Contains(a.Status.CodeName), ct);
    }

    /// <inheritdoc />
    public async Task<bool> StudentHasActiveApplicationAsync(Guid studentId, CancellationToken ct)
    {
        return await db.StudentApplications.AsNoTracking().AnyAsync(a =>
            a.StudentId == studentId && !TerminalStatuses.Contains(a.Status.CodeName), ct);
    }

    /// <inheritdoc />
    public async Task<int> CountOccupiedSlotsBySupervisorAsync(Guid supervisorUserId, CancellationToken ct)
    {
        var approvedByDepartmentHeadStatusId = await db.ApplicationStatuses
            .AsNoTracking()
            .Where(s => s.CodeName == "ApprovedByDepartmentHead")
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(ct);

        if (approvedByDepartmentHeadStatusId is null)
            return 0;

        return await db.StudentApplications.AsNoTracking()
            .Where(a =>
                a.SupervisorRequest != null &&
                a.SupervisorRequest.TeacherUserId == supervisorUserId &&
                a.StatusId == approvedByDepartmentHeadStatusId.Value)
            .CountAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Guid?> GetStudentIdByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return await db.Students.AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Teacher?> GetTeacherByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return await db.Teachers.AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);
    }

    public async Task<SupervisorRequest?> GetApprovedSupervisorRequestAsync(Guid supervisorRequestId, Guid studentId, CancellationToken ct)
    {
        return await db.SupervisorRequests.AsNoTracking()
            .Include(r => r.Status)
            .FirstOrDefaultAsync(r =>
                r.Id == supervisorRequestId &&
                r.StudentId == studentId &&
                r.Status.CodeName == "ApprovedBySupervisor", ct);
    }
}
