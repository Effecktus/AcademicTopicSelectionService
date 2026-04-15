using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Application.Abstractions;

/// <summary>
/// Абстракция объектного хранилища (S3/MinIO): presigned URL и проверка объектов.
/// </summary>
public interface IFileStorageService
{
    Task<FileUrlDto> GenerateUploadUrlAsync(string objectKey, TimeSpan expiresIn, CancellationToken ct);

    Task<FileUrlDto> GenerateDownloadUrlAsync(string objectKey, TimeSpan expiresIn, string? fileName, CancellationToken ct);

    Task<bool> ObjectExistsAsync(string objectKey, CancellationToken ct);

    Task DeleteObjectAsync(string objectKey, CancellationToken ct);
}
