using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Notifications;
using AcademicTopicSelectionService.Domain.Entities;

namespace AcademicTopicSelectionService.Application.GraduateWorks;

/// <summary>
/// Сервис архива ВКР: метаданные в БД, файлы через <see cref="IFileStorageService"/>.
/// </summary>
public sealed class GraduateWorksService(
    IGraduateWorksRepository repo,
    IFileStorageService fileStorage,
    INotificationsService notificationsService)
    : IGraduateWorksService
{
    private static readonly TimeSpan PresignedUrlLifetime = TimeSpan.FromMinutes(15);

    /// <inheritdoc />
    public Task<PagedResult<GraduateWorkDto>> GetAllAsync(ListGraduateWorksQuery query, CancellationToken ct)
    {
        var normalized = query with
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 200)
        };
        return repo.ListAsync(normalized, ct);
    }

    /// <inheritdoc />
    public Task<GraduateWorkDto?> GetByIdAsync(Guid id, CancellationToken ct) => repo.GetByIdAsync(id, ct);

    /// <inheritdoc />
    public async Task<Result<GraduateWorkDto, GraduateWorksError>> CreateAsync(
        CreateGraduateWorkCommand command, CancellationToken ct)
    {
        var err = ValidateMetadata(command.Title, command.Year, command.Grade, command.CommissionMembers);
        if (err is not null)
            return Result<GraduateWorkDto, GraduateWorksError>.Fail(GraduateWorksError.Validation, err);

        if (command.ApplicationId == Guid.Empty)
            return Result<GraduateWorkDto, GraduateWorksError>.Fail(GraduateWorksError.Validation, "ApplicationId is required");

        if (await repo.ExistsForApplicationAsync(command.ApplicationId, ct))
            return Result<GraduateWorkDto, GraduateWorksError>.Fail(GraduateWorksError.Validation,
                "Graduate work already exists for this application");

        var ctx = await repo.GetArchiveContextByApplicationIdAsync(command.ApplicationId, ct);
        if (ctx is null)
            return Result<GraduateWorkDto, GraduateWorksError>.Fail(GraduateWorksError.Validation,
                "Application not found or supervisor is not linked to a teacher profile");

        var title = command.Title.Trim();
        var commission = command.CommissionMembers.Trim();

        var entity = new GraduateWork
        {
            ApplicationId = command.ApplicationId,
            StudentId = ctx.StudentId,
            TeacherId = ctx.TeacherId,
            Title = title,
            Year = command.Year,
            Grade = command.Grade,
            CommissionMembers = commission,
            FilePath = null,
            PresentationPath = null,
            CreatedAt = DateTime.UtcNow
        };

        var saved = await repo.AddAsync(entity, ct);
        var dto = await repo.GetByIdAsync(saved.Id, ct);
        return Result<GraduateWorkDto, GraduateWorksError>.Ok(dto!);
    }

    /// <inheritdoc />
    public async Task<Result<GraduateWorkDto, GraduateWorksError>> UpdateAsync(
        UpdateGraduateWorkCommand command, CancellationToken ct)
    {
        var err = ValidateMetadata(command.Title, command.Year, command.Grade, command.CommissionMembers);
        if (err is not null)
            return Result<GraduateWorkDto, GraduateWorksError>.Fail(GraduateWorksError.Validation, err);

        var entity = await repo.GetByIdTrackedAsync(command.Id, ct);
        if (entity is null)
            return Result<GraduateWorkDto, GraduateWorksError>.Fail(GraduateWorksError.NotFound, "Graduate work not found");

        entity.Title = command.Title.Trim();
        entity.Year = command.Year;
        entity.Grade = command.Grade;
        entity.CommissionMembers = command.CommissionMembers.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await repo.SaveChangesAsync(ct);
        var dto = await repo.GetByIdAsync(command.Id, ct);
        return Result<GraduateWorkDto, GraduateWorksError>.Ok(dto!);
    }

    /// <inheritdoc />
    public async Task<Result<Unit, GraduateWorksError>> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await repo.GetByIdTrackedAsync(id, ct);
        if (entity is null)
            return Result<Unit, GraduateWorksError>.Fail(GraduateWorksError.NotFound, "Graduate work not found");

        if (!string.IsNullOrWhiteSpace(entity.FilePath))
            await fileStorage.DeleteObjectAsync(entity.FilePath, ct);

        if (!string.IsNullOrWhiteSpace(entity.PresentationPath))
            await fileStorage.DeleteObjectAsync(entity.PresentationPath, ct);

        await repo.DeleteAsync(entity, ct);
        return Result<Unit, GraduateWorksError>.Ok(Unit.Value);
    }

    /// <inheritdoc />
    public async Task<Result<FileUrlDto, GraduateWorksError>> GetUploadUrlAsync(
        Guid graduateWorkId, string fileType, CancellationToken ct)
    {
        if (!GraduateWorksFileTypes.TryNormalize(fileType, out var normalized))
            return Result<FileUrlDto, GraduateWorksError>.Fail(GraduateWorksError.Validation,
                "fileType must be 'thesis' or 'presentation'");

        var exists = await repo.GetByIdAsync(graduateWorkId, ct);
        if (exists is null)
            return Result<FileUrlDto, GraduateWorksError>.Fail(GraduateWorksError.NotFound, "Graduate work not found");

        var objectKey = BuildObjectKey(graduateWorkId, normalized);
        var url = await fileStorage.GenerateUploadUrlAsync(objectKey, PresignedUrlLifetime, ct);
        return Result<FileUrlDto, GraduateWorksError>.Ok(url);
    }

    /// <inheritdoc />
    public async Task<Result<FileUrlDto, GraduateWorksError>> GetDownloadUrlAsync(
        Guid graduateWorkId, string fileType, CancellationToken ct)
    {
        if (!GraduateWorksFileTypes.TryNormalize(fileType, out var normalized))
            return Result<FileUrlDto, GraduateWorksError>.Fail(GraduateWorksError.Validation,
                "fileType must be 'thesis' or 'presentation'");

        var entity = await repo.GetByIdTrackedAsync(graduateWorkId, ct);
        if (entity is null)
            return Result<FileUrlDto, GraduateWorksError>.Fail(GraduateWorksError.NotFound, "Graduate work not found");

        var objectKey = normalized == GraduateWorksFileTypes.Thesis ? entity.FilePath : entity.PresentationPath;

        if (string.IsNullOrWhiteSpace(objectKey))
            return Result<FileUrlDto, GraduateWorksError>.Fail(GraduateWorksError.Validation, "File not uploaded");

        var fileName = normalized == GraduateWorksFileTypes.Thesis ? entity.FileName : entity.PresentationFileName;
        var url = await fileStorage.GenerateDownloadUrlAsync(objectKey, PresignedUrlLifetime, fileName, ct);
        return Result<FileUrlDto, GraduateWorksError>.Ok(url);
    }

    /// <inheritdoc />
    public async Task<Result<Unit, GraduateWorksError>> ConfirmUploadAsync(
        Guid graduateWorkId, string fileType, string fileName, CancellationToken ct)
    {
        if (!GraduateWorksFileTypes.TryNormalize(fileType, out var normalized))
            return Result<Unit, GraduateWorksError>.Fail(GraduateWorksError.Validation,
                "fileType must be 'thesis' or 'presentation'");

        if (string.IsNullOrWhiteSpace(fileName))
            return Result<Unit, GraduateWorksError>.Fail(GraduateWorksError.Validation, "FileName is required");

        if (fileName.Trim().Length > 255)
            return Result<Unit, GraduateWorksError>.Fail(GraduateWorksError.Validation, "FileName must be <= 255 characters");

        var entity = await repo.GetByIdTrackedAsync(graduateWorkId, ct);
        if (entity is null)
            return Result<Unit, GraduateWorksError>.Fail(GraduateWorksError.NotFound, "Graduate work not found");

        var objectKey = BuildObjectKey(graduateWorkId, normalized);
        if (!await fileStorage.ObjectExistsAsync(objectKey, ct))
            return Result<Unit, GraduateWorksError>.Fail(GraduateWorksError.Validation, "Object not found in storage");

        if (normalized == GraduateWorksFileTypes.Thesis)
        {
            entity.FilePath = objectKey;
            entity.FileName = fileName.Trim();
        }
        else
        {
            entity.PresentationPath = objectKey;
            entity.PresentationFileName = fileName.Trim();
        }

        entity.UpdatedAt = DateTime.UtcNow;
        var studentUserId = await repo.GetStudentUserIdByStudentProfileIdAsync(entity.StudentId, ct);
        if (studentUserId is { } uid)
        {
            var fileKind = normalized == GraduateWorksFileTypes.Thesis ? "текст ВКР" : "презентация";
            var notification = await notificationsService.CreateAsync(
                new CreateNotificationCommand(
                    uid,
                    NotificationTypeCodes.GraduateWorkUploaded,
                    "Файл ВКР загружен",
                    $"По работе «{entity.Title}» подтверждена загрузка: {fileKind} ({fileName.Trim()})."),
                ct);

            await repo.SaveChangesAsync(ct);

            if (notification is not null)
            {
                await notificationsService.EnqueueEmailAsync(
                    notification.UserId,
                    notification.Title,
                    notification.Content,
                    ct);
            }
        }
        else
        {
            await repo.SaveChangesAsync(ct);
        }

        return Result<Unit, GraduateWorksError>.Ok(Unit.Value);
    }

    private static string BuildObjectKey(Guid graduateWorkId, string normalizedFileType) =>
        $"graduate-works/{graduateWorkId:D}/{normalizedFileType}";

    private static string? ValidateMetadata(string title, int year, int grade, string commissionMembers)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Title is required";
        if (title.Trim().Length > 500)
            return "Title must be <= 500 characters";

        if (string.IsNullOrWhiteSpace(commissionMembers))
            return "CommissionMembers is required";

        if (year is < 2000 or > 2100)
            return "Year must be between 2000 and 2100";

        if (grade is < 0 or > 100)
            return "Grade must be between 0 and 100";

        return null;
    }
}
