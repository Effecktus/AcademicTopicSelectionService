using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.GraduateWorks;
using AcademicTopicSelectionService.Domain.Entities;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Репозиторий архива выпускных квалификационных работ.
/// </summary>
public interface IGraduateWorksRepository
{
    Task<PagedResult<GraduateWorkDto>> ListAsync(ListGraduateWorksQuery query, CancellationToken ct);

    Task<GraduateWorkDto?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<GraduateWork?> GetByIdTrackedAsync(Guid id, CancellationToken ct);

    Task<bool> ExistsForApplicationAsync(Guid applicationId, CancellationToken ct);

    Task<GraduateWorkArchiveContext?> GetArchiveContextByApplicationIdAsync(Guid applicationId, CancellationToken ct);

    Task<GraduateWork> AddAsync(GraduateWork entity, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);

    Task DeleteAsync(GraduateWork entity, CancellationToken ct);
}
