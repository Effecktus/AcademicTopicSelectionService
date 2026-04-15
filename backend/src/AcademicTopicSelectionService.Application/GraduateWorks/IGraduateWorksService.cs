using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.GraduateWorks;

/// <summary>
/// Сервис архива выпускных квалификационных работ.
/// </summary>
public interface IGraduateWorksService
{
    Task<PagedResult<GraduateWorkDto>> GetAllAsync(ListGraduateWorksQuery query, CancellationToken ct);

    Task<GraduateWorkDto?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<Result<GraduateWorkDto, GraduateWorksError>> CreateAsync(CreateGraduateWorkCommand command, CancellationToken ct);

    Task<Result<GraduateWorkDto, GraduateWorksError>> UpdateAsync(UpdateGraduateWorkCommand command, CancellationToken ct);

    Task<Result<Unit, GraduateWorksError>> DeleteAsync(Guid id, CancellationToken ct);

    Task<Result<FileUrlDto, GraduateWorksError>> GetUploadUrlAsync(Guid graduateWorkId, string fileType, CancellationToken ct);

    Task<Result<FileUrlDto, GraduateWorksError>> GetDownloadUrlAsync(Guid graduateWorkId, string fileType, CancellationToken ct);

    Task<Result<Unit, GraduateWorksError>> ConfirmUploadAsync(Guid graduateWorkId, string fileType, string fileName, CancellationToken ct);
}

/// <summary>
/// Маркер успешной операции без полезной нагрузки.
/// </summary>
public readonly struct Unit
{
    public static Unit Value => default;
}
