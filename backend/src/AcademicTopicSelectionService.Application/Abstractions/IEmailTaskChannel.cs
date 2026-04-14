namespace AcademicTopicSelectionService.Application.Abstractions;

public sealed record EmailTask(string ToEmail, string Subject, string Body);

public interface IEmailTaskChannel
{
    ValueTask WriteAsync(EmailTask task, CancellationToken ct);

    IAsyncEnumerable<EmailTask> ReadAllAsync(CancellationToken ct);
}
