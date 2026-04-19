using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Notifications;
using AcademicTopicSelectionService.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.Notifications;

public sealed class NotificationsServiceTests
{
    private readonly INotificationsRepository _repo = Substitute.For<INotificationsRepository>();
    private readonly IUsersRepository _usersRepo = Substitute.For<IUsersRepository>();
    private readonly IEmailTaskChannel _emailTaskChannel = Substitute.For<IEmailTaskChannel>();
    private readonly NotificationsService _sut;

    public NotificationsServiceTests()
    {
        _sut = new NotificationsService(_repo, _usersRepo, _emailTaskChannel);
    }

    [Fact]
    public async Task GetForCurrentUserAsync_NormalizesPageAndPageSize()
    {
        _repo.ListByUserAsync(Arg.Any<Guid>(), Arg.Any<ListNotificationsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<NotificationDto>(1, 200, 0, []));

        await _sut.GetForCurrentUserAsync(new ListNotificationsQuery(null, 0, 999), Guid.NewGuid(), CancellationToken.None);

        await _repo.Received(1).ListByUserAsync(
            Arg.Any<Guid>(),
            Arg.Is<ListNotificationsQuery>(q => q.Page == 1 && q.PageSize == 200),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAsReadAsync_ReturnsForbidden_ForForeignNotification()
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            IsRead = false
        };
        _repo.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);

        var result = await _sut.MarkAsReadAsync(notification.Id, Guid.NewGuid(), CancellationToken.None);

        result.Error.Should().Be(NotificationsError.Forbidden);
    }

    [Fact]
    public async Task CreateAsync_ReturnsNull_WhenTypeNotFound()
    {
        _repo.GetTypeByCodeNameAsync("Missing", Arg.Any<CancellationToken>())
            .Returns((NotificationType?)null);

        var created = await _sut.CreateAsync(
            new CreateNotificationCommand(Guid.NewGuid(), "Missing", "Title", "Content"),
            CancellationToken.None);

        created.Should().BeNull();
        _repo.DidNotReceive().Add(Arg.Any<Notification>());
    }

    [Fact]
    public async Task CreateAndSaveAsync_CallsSaveChanges_WhenTypeExists()
    {
        var typeId = Guid.NewGuid();
        _repo.GetTypeByCodeNameAsync("NewMessage", Arg.Any<CancellationToken>())
            .Returns(new NotificationType
            {
                Id = typeId,
                CodeName = "NewMessage",
                DisplayName = "Новое сообщение"
            });

        var userId = Guid.NewGuid();
        var created = await _sut.CreateAndSaveAsync(
            new CreateNotificationCommand(userId, "NewMessage", "Заголовок", "Текст"),
            CancellationToken.None);

        created.Should().NotBeNull();
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnqueueEmailAsync_WritesToChannel_WhenUserWithEmailExists()
    {
        var userId = Guid.NewGuid();
        _usersRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new User
            {
                Id = userId,
                Email = "user@test.local",
                Role = new UserRole { CodeName = "Student", DisplayName = "Студент" }
            });

        await _sut.EnqueueEmailAsync(userId, "Title", "Body", CancellationToken.None);

        await _emailTaskChannel.Received(1).WriteAsync(
            Arg.Is<EmailTask>(t => t.ToEmail == "user@test.local" && t.Subject == "Title" && t.Body == "Body"),
            Arg.Any<CancellationToken>());
    }
}
