namespace AcademicTopicSelectionService.Infrastructure.Storage;

public sealed class S3Options
{
    public const string SectionName = "S3";

    public string Provider { get; set; } = "Development";
    public string Endpoint { get; set; } = string.Empty;
    public string? PublicEndpoint { get; set; }
    public string Region { get; set; } = "us-east-1";
    public string BucketName { get; set; } = "graduate-works";
    public bool ForcePathStyle { get; set; } = true;

    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? AccessKeyFile { get; set; }
    public string? SecretKeyFile { get; set; }
}
