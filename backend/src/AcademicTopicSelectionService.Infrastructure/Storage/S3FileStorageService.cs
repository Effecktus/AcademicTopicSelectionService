using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using Microsoft.Extensions.Options;

namespace AcademicTopicSelectionService.Infrastructure.Storage;

/// <summary>
/// Объектное хранилище на базе S3 API (AWS S3 / MinIO).
/// Принимает два клиента через DI: внутренний (для операций с бакетом/объектами)
/// и presign-клиент (для генерации ссылок с публичным хостом).
/// </summary>
public sealed class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly IAmazonS3 _presignS3;
    private readonly string _bucketName;
    private volatile bool _bucketEnsured;

    // AWS SDK v4 принудительно использует https в presigned URL даже при ServiceURL с http://.
    // Подпись не включает схему, поэтому после генерации безопасно заменяем схему.
    private readonly string? _presignSchemeOverride;

    public S3FileStorageService(IAmazonS3 s3, PresignAmazonS3 presignS3, IOptions<S3Options> options)
    {
        _s3 = s3;
        _presignS3 = presignS3.Client;
        _bucketName = options.Value.BucketName;

        var publicEndpoint = options.Value.PublicEndpoint;
        if (!string.IsNullOrWhiteSpace(publicEndpoint) &&
            publicEndpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            _presignSchemeOverride = "http";
        }
    }

    /// <inheritdoc />
    public async Task<FileUrlDto> GenerateUploadUrlAsync(string objectKey, TimeSpan expiresIn, CancellationToken ct)
    {
        await EnsureBucketExistsAsync(ct);

        var expiresAt = DateTime.UtcNow.Add(expiresIn);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = expiresAt
        };

        var url = FixScheme(await _presignS3.GetPreSignedURLAsync(request));
        return new FileUrlDto(url, expiresAt);
    }

    /// <inheritdoc />
    public async Task<FileUrlDto> GenerateDownloadUrlAsync(string objectKey, TimeSpan expiresIn, string? fileName, CancellationToken ct)
    {
        await EnsureBucketExistsAsync(ct);

        var expiresAt = DateTime.UtcNow.Add(expiresIn);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = expiresAt
        };

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            // RFC 5987: filename* с UTF-8 кодированием для корректного отображения кириллицы
            var encoded = Uri.EscapeDataString(fileName);
            request.ResponseHeaderOverrides = new ResponseHeaderOverrides
            {
                ContentDisposition = $"attachment; filename=\"{fileName}\"; filename*=UTF-8''{encoded}"
            };
        }

        var url = FixScheme(await _presignS3.GetPreSignedURLAsync(request));
        return new FileUrlDto(url, expiresAt);
    }

    /// <inheritdoc />
    public async Task<bool> ObjectExistsAsync(string objectKey, CancellationToken ct)
    {
        try
        {
            await _s3.GetObjectMetadataAsync(_bucketName, objectKey, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (
            ex.StatusCode == System.Net.HttpStatusCode.NotFound ||
            string.Equals(ex.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ex.ErrorCode, "NotFound", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task DeleteObjectAsync(string objectKey, CancellationToken ct)
    {
        await _s3.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = objectKey
        }, ct);
    }

    private async Task EnsureBucketExistsAsync(CancellationToken ct)
    {
        if (_bucketEnsured)
            return;

        try
        {
            await _s3.GetBucketAclAsync(new GetBucketAclRequest { BucketName = _bucketName }, ct);
        }
        catch (AmazonS3Exception ex) when (
            ex.StatusCode == System.Net.HttpStatusCode.NotFound ||
            string.Equals(ex.ErrorCode, "NoSuchBucket", StringComparison.OrdinalIgnoreCase))
        {
            await _s3.PutBucketAsync(new PutBucketRequest { BucketName = _bucketName }, ct);
        }

        _bucketEnsured = true;
    }

    private string FixScheme(string url)
    {
        if (_presignSchemeOverride is null)
            return url;

        if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return _presignSchemeOverride + "://" + url["https://".Length..];

        return url;
    }

    internal static IAmazonS3 CreatePresignClient(S3Options options)
    {
        // ServiceURL и RegionEndpoint нельзя задавать одновременно — AWS SDK v4 бросает исключение.
        // При кастомном PublicEndpoint (MinIO) RegionEndpoint не нужен.
        var config = new AmazonS3Config
        {
            ForcePathStyle = options.ForcePathStyle,
            ServiceURL = options.PublicEndpoint
        };

        return new AmazonS3Client(
            new BasicAWSCredentials(options.AccessKey!, options.SecretKey!),
            config);
    }
}
