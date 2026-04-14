using AcademicTopicSelectionService.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AcademicTopicSelectionService.Infrastructure.Email;

public sealed class EmailBackgroundService(
    IEmailTaskChannel channel,
    IServiceScopeFactory scopeFactory,
    ILogger<EmailBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var task in channel.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                await emailSender.SendAsync(task.ToEmail, task.Subject, task.Body, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send email to {ToEmail}", task.ToEmail);
            }
        }
    }
}
