using System.Net;
using System.Net.Mail;
using AcademicTopicSelectionService.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AcademicTopicSelectionService.Infrastructure.Email;

public sealed class SmtpEmailSender(
    IOptions<SmtpOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            logger.LogWarning("SMTP is not configured. Skipping email to {ToEmail}", toEmail);
            return;
        }

        using var message = new MailMessage(_options.FromAddress, toEmail, subject, body);
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        // SmtpClient не поддерживает CancellationToken, поэтому оборачиваем в WaitAsync.
        await client.SendMailAsync(message).WaitAsync(ct);
        logger.LogInformation("Email sent via SMTP to {ToEmail}", toEmail);
    }
}
