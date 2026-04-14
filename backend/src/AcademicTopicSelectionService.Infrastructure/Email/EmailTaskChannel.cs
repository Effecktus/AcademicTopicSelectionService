using System.Threading.Channels;
using AcademicTopicSelectionService.Application.Abstractions;

namespace AcademicTopicSelectionService.Infrastructure.Email;

public sealed class EmailTaskChannel : IEmailTaskChannel
{
    private readonly Channel<EmailTask> _channel = Channel.CreateUnbounded<EmailTask>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

    public ValueTask WriteAsync(EmailTask task, CancellationToken ct)
        => _channel.Writer.WriteAsync(task, ct);

    public IAsyncEnumerable<EmailTask> ReadAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}
