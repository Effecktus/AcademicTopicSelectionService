using Amazon.S3;

namespace AcademicTopicSelectionService.Infrastructure.Storage;

/// <summary>
/// Маркерная обёртка над <see cref="IAmazonS3"/> для клиента, генерирующего presigned URL
/// с публичным хостом (<c>S3:PublicEndpoint</c>). Регистрируется как Singleton.
/// </summary>
public sealed class PresignAmazonS3(IAmazonS3 client) : IDisposable
{
    public IAmazonS3 Client { get; } = client;

    public void Dispose() => Client.Dispose();
}
