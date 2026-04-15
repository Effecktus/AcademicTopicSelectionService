using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;

namespace AcademicTopicSelectionService.Infrastructure.Storage;

/// <summary>
/// Заглушка объектного хранилища для разработки и тестов без MinIO/S3.
/// Генерирует фиктивные URL; <see cref="ObjectExistsAsync"/> всегда возвращает <c>true</c>.
/// </summary>
public sealed class DevelopmentFileStorageService : IFileStorageService
{
    /// <inheritdoc />
    public Task<FileUrlDto> GenerateUploadUrlAsync(string objectKey, TimeSpan expiresIn, CancellationToken ct)
    {
        var expiresAt = DateTime.UtcNow.Add(expiresIn);
        var url = $"https://stub-storage.invalid/upload/{Uri.EscapeDataString(objectKey)}";
        return Task.FromResult(new FileUrlDto(url, expiresAt));
    }

    /// <inheritdoc />
    public Task<FileUrlDto> GenerateDownloadUrlAsync(string objectKey, TimeSpan expiresIn, string? fileName, CancellationToken ct)
    {
        var expiresAt = DateTime.UtcNow.Add(expiresIn);
        var url = $"https://stub-storage.invalid/download/{Uri.EscapeDataString(objectKey)}";
        return Task.FromResult(new FileUrlDto(url, expiresAt));
    }

    /// <inheritdoc />
    public Task<bool> ObjectExistsAsync(string objectKey, CancellationToken ct) => Task.FromResult(true);

    /// <inheritdoc />
    public Task DeleteObjectAsync(string objectKey, CancellationToken ct) => Task.CompletedTask;
}
