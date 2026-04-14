using System.Collections.Concurrent;
using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Infrastructure.Email;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.Notifications;

public sealed class EmailBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_CallsEmailSender_WhenTaskReceived()
    {
        var channel = new EmailTaskChannel();
        var sender = new CapturingEmailSender();
        using var provider = BuildServiceProvider(sender);

        var sut = new EmailBackgroundService(
            channel,
            provider.GetRequiredService<IServiceScopeFactory>(),
            Substitute.For<ILogger<EmailBackgroundService>>());

        await sut.StartAsync(CancellationToken.None);

        await channel.WriteAsync(new EmailTask("teacher@test.com", "Subject", "Body"), CancellationToken.None);
        var sent = await sender.WaitForEmailAsync(TimeSpan.FromSeconds(2));

        sent.Should().NotBeNull();
        sent!.ToEmail.Should().Be("teacher@test.com");
        sent.Subject.Should().Be("Subject");
        sent.Body.Should().Be("Body");

        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_ContinuesProcessing_WhenFirstSendFails()
    {
        var channel = new EmailTaskChannel();
        var sender = new FlakyEmailSender();
        using var provider = BuildServiceProvider(sender);

        var sut = new EmailBackgroundService(
            channel,
            provider.GetRequiredService<IServiceScopeFactory>(),
            Substitute.For<ILogger<EmailBackgroundService>>());

        await sut.StartAsync(CancellationToken.None);

        await channel.WriteAsync(new EmailTask("first@test.com", "First", "Body1"), CancellationToken.None);
        await channel.WriteAsync(new EmailTask("second@test.com", "Second", "Body2"), CancellationToken.None);

        var sent = await sender.WaitForSecondEmailAsync(TimeSpan.FromSeconds(2));
        sent.Should().NotBeNull();
        sent!.ToEmail.Should().Be("second@test.com");
        sender.Attempts.Should().Be(2);

        await sut.StopAsync(CancellationToken.None);
    }

    private static ServiceProvider BuildServiceProvider(IEmailSender sender)
    {
        var services = new ServiceCollection();
        services.AddScoped<IEmailSender>(_ => sender);
        return services.BuildServiceProvider();
    }

    private sealed class CapturingEmailSender : IEmailSender
    {
        private readonly TaskCompletionSource<EmailTask> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
        {
            _tcs.TrySetResult(new EmailTask(toEmail, subject, body));
            return Task.CompletedTask;
        }

        public async Task<EmailTask?> WaitForEmailAsync(TimeSpan timeout)
        {
            var completed = await Task.WhenAny(_tcs.Task, Task.Delay(timeout));
            return completed == _tcs.Task ? await _tcs.Task : null;
        }
    }

    private sealed class FlakyEmailSender : IEmailSender
    {
        private readonly ConcurrentQueue<EmailTask> _sent = new();
        private readonly TaskCompletionSource<EmailTask> _secondEmailTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Attempts { get; private set; }

        public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
        {
            Attempts++;
            if (Attempts == 1)
                throw new InvalidOperationException("Simulated SMTP failure");

            var task = new EmailTask(toEmail, subject, body);
            _sent.Enqueue(task);
            _secondEmailTcs.TrySetResult(task);
            return Task.CompletedTask;
        }

        public async Task<EmailTask?> WaitForSecondEmailAsync(TimeSpan timeout)
        {
            var completed = await Task.WhenAny(_secondEmailTcs.Task, Task.Delay(timeout));
            return completed == _secondEmailTcs.Task ? await _secondEmailTcs.Task : null;
        }
    }
}
