using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.ChatMessages;
using AcademicTopicSelectionService.Application.StudentApplications;
using AcademicTopicSelectionService.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.ChatMessages;

public sealed class ChatMessagesServiceTests
{
    private static readonly Guid ApplicationId = Guid.NewGuid();
    private static readonly Guid StudentUserId = Guid.NewGuid();
    private static readonly Guid TeacherUserId = Guid.NewGuid();

    private readonly IStudentApplicationsRepository _apps = Substitute.For<IStudentApplicationsRepository>();
    private readonly IChatMessagesRepository _chat = Substitute.For<IChatMessagesRepository>();

    private ChatMessagesService CreateSut() => new(_apps, _chat);

    private static ApplicationChatAccessInfo ApprovedAccess() =>
        new(StudentUserId, TeacherUserId, "ApprovedBySupervisor", true);

    [Fact]
    public async Task SendMessageAsync_ReturnsNotFound_WhenApplicationMissing()
    {
        _apps.GetChatAccessAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns((ApplicationChatAccessInfo?)null);

        var sut = CreateSut();
        var result = await sut.SendMessageAsync(
            new SendMessageCommand(ApplicationId, "hi"), StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ChatMessagesError.NotFound);
        await _chat.DidNotReceive().AddAsync(Arg.Any<ChatMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendMessageAsync_ReturnsOk_WhenStudentSends_AndPersistsViaRepository()
    {
        _apps.GetChatAccessAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(ApprovedAccess());

        _chat.AddAsync(Arg.Any<ChatMessage>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var m = ci.ArgAt<ChatMessage>(0);
                m.Sender = new User
                {
                    Id = m.SenderId,
                    Email = "s@test.com",
                    PasswordHash = "x",
                    FirstName = "Иван",
                    LastName = "Студент",
                    RoleId = Guid.NewGuid()
                };
                return Task.FromResult(m);
            });

        var sut = CreateSut();
        var result = await sut.SendMessageAsync(
            new SendMessageCommand(ApplicationId, "  Привет  "), StudentUserId, CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value!.Content.Should().Be("Привет");
        result.Value.SenderId.Should().Be(StudentUserId);
        result.Value.SenderFullName.Should().Contain("Иван");

        await _chat.Received(1).AddAsync(
            Arg.Is<ChatMessage>(m =>
                m.ApplicationId == ApplicationId &&
                m.SenderId == StudentUserId &&
                m.Content == "Привет"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendMessageAsync_ReturnsForbidden_WhenUserIsNotParticipant()
    {
        _apps.GetChatAccessAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(ApprovedAccess());

        var sut = CreateSut();
        var result = await sut.SendMessageAsync(
            new SendMessageCommand(ApplicationId, "hi"), Guid.NewGuid(), CancellationToken.None);

        result.Error.Should().Be(ChatMessagesError.Forbidden);
        await _chat.DidNotReceive().AddAsync(Arg.Any<ChatMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendMessageAsync_ReturnsValidation_WhenContentEmpty()
    {
        _apps.GetChatAccessAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(ApprovedAccess());

        var sut = CreateSut();
        var result = await sut.SendMessageAsync(
            new SendMessageCommand(ApplicationId, "   "), StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ChatMessagesError.Validation);
        await _chat.DidNotReceive().AddAsync(Arg.Any<ChatMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendMessageAsync_ReturnsValidation_WhenContentTooLong()
    {
        _apps.GetChatAccessAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(ApprovedAccess());

        var sut = CreateSut();
        var result = await sut.SendMessageAsync(
            new SendMessageCommand(ApplicationId, new string('x', 4001)), StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ChatMessagesError.Validation);
    }

    [Fact]
    public async Task SendMessageAsync_ReturnsForbidden_WhenSupervisorRequestRejected()
    {
        _apps.GetChatAccessAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(new ApplicationChatAccessInfo(StudentUserId, TeacherUserId, "RejectedBySupervisor", true));

        var sut = CreateSut();
        var result = await sut.SendMessageAsync(
            new SendMessageCommand(ApplicationId, "hi"), StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ChatMessagesError.Forbidden);
    }

    [Fact]
    public async Task MarkAsReadAsync_ReturnsNotFound_WhenApplicationMissing()
    {
        _apps.GetChatAccessAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns((ApplicationChatAccessInfo?)null);

        var sut = CreateSut();
        var result = await sut.MarkAsReadAsync(ApplicationId, StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ChatMessagesError.NotFound);
        await _chat.DidNotReceive().MarkIncomingAsReadAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAsReadAsync_ReturnsForbidden_WhenUserIsNotParticipant()
    {
        _apps.GetChatAccessAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(ApprovedAccess());

        var sut = CreateSut();
        var result = await sut.MarkAsReadAsync(ApplicationId, Guid.NewGuid(), CancellationToken.None);

        result.Error.Should().Be(ChatMessagesError.Forbidden);
        await _chat.DidNotReceive().MarkIncomingAsReadAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAsReadAsync_ReturnsForbidden_WhenChatUnavailable()
    {
        _apps.GetChatAccessAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(new ApplicationChatAccessInfo(StudentUserId, TeacherUserId, "Cancelled", true));

        var sut = CreateSut();
        var result = await sut.MarkAsReadAsync(ApplicationId, StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ChatMessagesError.Forbidden);
        await _chat.DidNotReceive().MarkIncomingAsReadAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAsReadAsync_CallsRepository_WithReaderId_FilteringOutgoingInSql()
    {
        _apps.GetChatAccessAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(ApprovedAccess());

        var sut = CreateSut();
        await sut.MarkAsReadAsync(ApplicationId, StudentUserId, CancellationToken.None);

        await _chat.Received(1).MarkIncomingAsReadAsync(ApplicationId, StudentUserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMessagesAsync_ReturnsNotFound_WhenApplicationMissing()
    {
        _apps.GetChatAccessAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns((ApplicationChatAccessInfo?)null);

        var sut = CreateSut();
        var result = await sut.GetMessagesAsync(ApplicationId, null, 50, StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ChatMessagesError.NotFound);
        await _chat.DidNotReceive().GetByApplicationAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMessagesAsync_ReturnsForbidden_WhenUserIsNotParticipant()
    {
        _apps.GetChatAccessAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(ApprovedAccess());

        var sut = CreateSut();
        var result = await sut.GetMessagesAsync(ApplicationId, null, 50, Guid.NewGuid(), CancellationToken.None);

        result.Error.Should().Be(ChatMessagesError.Forbidden);
        await _chat.DidNotReceive().GetByApplicationAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMessagesAsync_ReturnsForbidden_WhenChatUnavailable()
    {
        _apps.GetChatAccessAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(new ApplicationChatAccessInfo(StudentUserId, TeacherUserId, "Cancelled", true));

        var sut = CreateSut();
        var result = await sut.GetMessagesAsync(ApplicationId, null, 50, StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ChatMessagesError.Forbidden);
        await _chat.DidNotReceive().GetByApplicationAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMessagesAsync_WithAfterId_DelegatesToRepository()
    {
        var after = Guid.NewGuid();
        _apps.GetChatAccessAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(ApprovedAccess());

        var msg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ApplicationId = ApplicationId,
            SenderId = StudentUserId,
            Content = "x",
            SentAt = DateTime.UtcNow,
            Sender = new User
            {
                Id = StudentUserId,
                Email = "a@b.c",
                PasswordHash = "x",
                FirstName = "A",
                LastName = "B",
                RoleId = Guid.NewGuid()
            }
        };
        _chat.GetByApplicationAsync(ApplicationId, after, 50, Arg.Any<CancellationToken>())
            .Returns(new List<ChatMessage> { msg });

        var sut = CreateSut();
        var result = await sut.GetMessagesAsync(ApplicationId, after, 50, StudentUserId, CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().HaveCount(1);
        result.Value![0].Id.Should().Be(msg.Id);
    }
}
