using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.SupervisorRequests;

namespace AcademicTopicSelectionService.Application.Abstractions;

public interface ISupervisorRequestsService
{
    Task<PagedResult<SupervisorRequestDto>> ListForRoleAsync(
        ListSupervisorRequestsQuery query,
        string roleCodeName,
        Guid userId,
        CancellationToken ct);

    Task<SupervisorRequestDetailDto?> GetDetailAsync(Guid id, CancellationToken ct);

    Task<Result<SupervisorRequestDto, SupervisorRequestsError>> CreateAsync(
        CreateSupervisorRequestCommand command,
        Guid studentUserId,
        CancellationToken ct);

    Task<Result<SupervisorRequestDto, SupervisorRequestsError>> ApproveAsync(
        Guid id,
        ApproveSupervisorRequestCommand command,
        Guid teacherUserId,
        CancellationToken ct);

    Task<Result<SupervisorRequestDto, SupervisorRequestsError>> RejectAsync(
        Guid id,
        RejectSupervisorRequestCommand command,
        Guid teacherUserId,
        CancellationToken ct);

    Task<Result<bool, SupervisorRequestsError>> CancelAsync(
        Guid id,
        Guid studentUserId,
        CancellationToken ct);
}
