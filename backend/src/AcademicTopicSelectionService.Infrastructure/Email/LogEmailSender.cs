using AcademicTopicSelectionService.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace AcademicTopicSelectionService.Infrastructure.Email;

public sealed class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Email queued and sent via log sender. To: {ToEmail}; Subject: {Subject}; Body: {Body}",
            toEmail,
            subject,
            body);
        return Task.CompletedTask;
    }
}
