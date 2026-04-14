using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.SupervisorRequests;
using AcademicTopicSelectionService.Domain.Entities;

namespace AcademicTopicSelectionService.Application.Abstractions;

public interface ISupervisorRequestsRepository
{
    Task<PagedResult<SupervisorRequestDto>> ListForRoleAsync(
        ListSupervisorRequestsQuery query,
        string roleCodeName,
        Guid userId,
        CancellationToken ct);

    Task<SupervisorRequestDetailDto?> GetDetailAsync(Guid id, CancellationToken ct);

    Task<SupervisorRequest?> GetByIdWithTrackingAsync(Guid id, CancellationToken ct);

    Task<SupervisorRequest> AddAsync(SupervisorRequest request, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);

    Task<bool> HasActiveRequestForTeacherAsync(Guid studentId, Guid teacherUserId, CancellationToken ct);

    Task<int> CountActiveRequestsForStudentAsync(Guid studentId, CancellationToken ct);

    Task<int> CountTeachersInDepartmentAsync(Guid departmentId, CancellationToken ct);

    Task CancelAllActiveRequestsExceptAsync(Guid studentId, Guid approvedRequestId, CancellationToken ct);

    Task<IReadOnlyList<SupervisorRequest>> GetApprovedRequestsByStudentAsync(Guid studentId, CancellationToken ct);

    Task<Guid?> GetStudentIdByUserIdAsync(Guid userId, CancellationToken ct);
}
