using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.SupervisorRequests;
using AcademicTopicSelectionService.Domain.Entities;
using AcademicTopicSelectionService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicTopicSelectionService.Infrastructure.Repositories;

/// <summary>
/// Репозиторий запросов на выбор научного руководителя.
/// </summary>
public sealed class SupervisorRequestsRepository(ApplicationDbContext db) : ISupervisorRequestsRepository
{
    /// <inheritdoc />
    public async Task<PagedResult<SupervisorRequestDto>> ListForRoleAsync(
        ListSupervisorRequestsQuery query,
        string roleCodeName,
        Guid userId,
        CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var baseQuery = db.SupervisorRequests
            .AsNoTracking()
            .Include(r => r.Student).ThenInclude(s => s.User)
            .Include(r => r.TeacherUser)
            .Include(r => r.Status)
            .AsQueryable();

        baseQuery = roleCodeName switch
        {
            "Student" => baseQuery.Where(r => r.Student.UserId == userId),
            "Teacher" => baseQuery.Where(r => r.TeacherUserId == userId),
            "Admin" => baseQuery,
            _ => baseQuery.Where(_ => false)
        };

        var total = await baseQuery.LongCountAsync(ct);
        var items = await baseQuery
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new SupervisorRequestDto(
                r.Id,
                r.StudentId,
                r.Student.User.FirstName,
                r.Student.User.LastName,
                r.TeacherUserId,
                r.TeacherUser.FirstName,
                r.TeacherUser.LastName,
                new ApplicationStatusRefDto(r.Status.Id, r.Status.CodeName, r.Status.DisplayName),
                r.Comment,
                r.CreatedAt,
                r.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<SupervisorRequestDto>(page, pageSize, total, items);
    }

    /// <inheritdoc />
    public async Task<SupervisorRequestDetailDto?> GetDetailAsync(Guid id, CancellationToken ct)
    {
        var entity = await db.SupervisorRequests
            .AsNoTracking()
            .Include(r => r.Student).ThenInclude(s => s.User)
            .Include(r => r.Student).ThenInclude(s => s.Group)
            .Include(r => r.TeacherUser)
            .Include(r => r.Status)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (entity is null)
            return null;

        return new SupervisorRequestDetailDto(
            entity.Id,
            entity.StudentId,
            entity.Student.User.FirstName,
            entity.Student.User.LastName,
            entity.Student.Group.CodeName.ToString(),
            entity.TeacherUserId,
            entity.TeacherUser.FirstName,
            entity.TeacherUser.LastName,
            entity.TeacherUser.Email,
            new ApplicationStatusRefDto(entity.Status.Id, entity.Status.CodeName, entity.Status.DisplayName),
            entity.Comment,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    /// <inheritdoc />
    public Task<SupervisorRequest?> GetByIdWithTrackingAsync(Guid id, CancellationToken ct)
    {
        return db.SupervisorRequests
            .Include(r => r.Status)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task<SupervisorRequest> AddAsync(SupervisorRequest request, CancellationToken ct)
    {
        await db.SupervisorRequests.AddAsync(request, ct);
        return request;
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken ct)
        => db.SaveChangesAsync(ct);

    /// <inheritdoc />
    public Task<bool> HasActiveRequestForTeacherAsync(Guid studentId, Guid teacherUserId, CancellationToken ct)
        => db.SupervisorRequests
            .AsNoTracking()
            .AnyAsync(r =>
                r.StudentId == studentId &&
                r.TeacherUserId == teacherUserId &&
                r.Status.CodeName == "Pending", ct);

    /// <inheritdoc />
    public Task<int> CountActiveRequestsForStudentAsync(Guid studentId, CancellationToken ct)
        => db.SupervisorRequests
            .AsNoTracking()
            .CountAsync(r => r.StudentId == studentId && r.Status.CodeName == "Pending", ct);

    /// <inheritdoc />
    public Task<int> CountTeachersInDepartmentAsync(Guid departmentId, CancellationToken ct)
        => db.Users
            .AsNoTracking()
            .CountAsync(u =>
                u.DepartmentId == departmentId &&
                u.IsActive &&
                u.Role.CodeName == "Teacher", ct);

    /// <inheritdoc />
    public async Task CancelAllActiveRequestsExceptAsync(Guid studentId, Guid approvedRequestId, CancellationToken ct)
    {
        var cancelledStatusId = await db.ApplicationStatuses
            .AsNoTracking()
            .Where(s => s.CodeName == "Cancelled")
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(ct);

        if (cancelledStatusId is null)
            return;

        var others = await db.SupervisorRequests
            .Where(r =>
                r.StudentId == studentId &&
                r.Id != approvedRequestId &&
                r.Status.CodeName == "Pending")
            .ToListAsync(ct);

        foreach (var item in others)
            item.StatusId = cancelledStatusId.Value;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SupervisorRequest>> GetApprovedRequestsByStudentAsync(Guid studentId, CancellationToken ct)
    {
        return await db.SupervisorRequests
            .AsNoTracking()
            .Where(r => r.StudentId == studentId && r.Status.CodeName == "ApprovedBySupervisor")
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Guid?> GetStudentIdByUserIdAsync(Guid userId, CancellationToken ct)
    {
        var studentId = await db.Students
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(ct);

        return studentId;
    }
}
