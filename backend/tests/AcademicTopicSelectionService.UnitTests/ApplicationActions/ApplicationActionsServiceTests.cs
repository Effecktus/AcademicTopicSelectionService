using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.ApplicationActions;
using AcademicTopicSelectionService.Application.Dictionaries;
using FluentAssertions;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.ApplicationActions;

public sealed class ApplicationActionsServiceTests
{
    private readonly IApplicationActionsRepository _repo = Substitute.For<IApplicationActionsRepository>();
    private readonly ApplicationActionsService _sut;

    private static readonly Guid PendingStatusId = Guid.NewGuid();
    private static readonly Guid ActorUserId = Guid.NewGuid();

    private static ApplicationActionsActor AdminActor() => new(Guid.NewGuid(), true);
    private static ApplicationActionsActor UserActor(Guid? userId = null) => new(userId ?? ActorUserId, false);

    public ApplicationActionsServiceTests()
    {
        _sut = new ApplicationActionsService(_repo);
        _repo.GetActionStatusIdByCodeNameAsync("Pending", Arg.Any<CancellationToken>())
            .Returns(PendingStatusId);
    }

    // -------------------------------------------------------------------------
    // ListByApplicationAsync — нормализация и доступ
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(2, 2)]
    public async Task ListByApplicationAsync_NormalizesPage(int inputPage, int expectedPage)
    {
        var appId = Guid.NewGuid();
        _repo.ApplicationExistsAsync(appId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.ListByApplicationAsync(Arg.Any<ListApplicationActionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ApplicationActionDto>(expectedPage, 50, 0, []));

        await _sut.ListByApplicationAsync(new ListApplicationActionsQuery(appId, inputPage, 50), AdminActor(),
            CancellationToken.None);

        await _repo.Received(1).ListByApplicationAsync(
            Arg.Is<ListApplicationActionsQuery>(q => q.Page == expectedPage),
            Arg.Any<CancellationToken>());
        _ = _repo.DidNotReceive().UserCanReadApplicationActionsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListByApplicationAsync_ReturnsApplicationNotFound_WhenApplicationMissing()
    {
        var appId = Guid.NewGuid();
        _repo.ApplicationExistsAsync(appId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.ListByApplicationAsync(new ListApplicationActionsQuery(appId, 1, 50), UserActor(),
            CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.ApplicationNotFound);
    }

    [Fact]
    public async Task ListByApplicationAsync_ReturnsForbidden_WhenNotReaderAndNotAdmin()
    {
        var appId = Guid.NewGuid();
        _repo.ApplicationExistsAsync(appId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.UserCanReadApplicationActionsAsync(appId, ActorUserId, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.ListByApplicationAsync(new ListApplicationActionsQuery(appId, 1, 50), UserActor(),
            CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.Forbidden);
        await _repo.DidNotReceive().ListByApplicationAsync(Arg.Any<ListApplicationActionsQuery>(),
            Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // CreateAsync — валидация и доступ
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenApplicationIdIsEmpty()
    {
        var result = await _sut.CreateAsync(
            new CreateApplicationActionCommand(Guid.Empty, Guid.NewGuid(), null), AdminActor(),
            CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.Validation);
        result.Message.Should().Contain("ApplicationId");
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenResponsibleIdIsEmpty()
    {
        var result = await _sut.CreateAsync(
            new CreateApplicationActionCommand(Guid.NewGuid(), Guid.Empty, null), AdminActor(),
            CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.Validation);
        result.Message.Should().Contain("ResponsibleId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_ReturnsValidationError_WhenCommentIsBlankString(string comment)
    {
        var result = await _sut.CreateAsync(
            new CreateApplicationActionCommand(Guid.NewGuid(), Guid.NewGuid(), comment), AdminActor(),
            CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.Validation);
        result.Message.Should().Contain("Comment");
    }

    [Fact]
    public async Task CreateAsync_ReturnsApplicationNotFound_WhenApplicationDoesNotExist()
    {
        var appId = Guid.NewGuid();
        _repo.ApplicationExistsAsync(appId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.CreateAsync(
            new CreateApplicationActionCommand(appId, Guid.NewGuid(), null), AdminActor(), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.ApplicationNotFound);
    }

    [Fact]
    public async Task CreateAsync_ReturnsForbidden_WhenNotReaderAndNotAdmin()
    {
        var appId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _repo.ApplicationExistsAsync(appId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.UserCanReadApplicationActionsAsync(appId, userId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.CreateAsync(
            new CreateApplicationActionCommand(appId, Guid.NewGuid(), null), UserActor(userId),
            CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.Forbidden);
    }

    [Fact]
    public async Task CreateAsync_ReturnsResponsibleUserNotFound_WhenUserDoesNotExist()
    {
        var appId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _repo.ApplicationExistsAsync(appId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.UserCanReadApplicationActionsAsync(appId, ActorUserId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.UserExistsAsync(userId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.CreateAsync(
            new CreateApplicationActionCommand(appId, userId, null), UserActor(), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.ResponsibleUserNotFound);
    }

    [Fact]
    public async Task CreateAsync_ReturnsStatusNotFound_WhenPendingStatusMissing()
    {
        var appId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _repo.ApplicationExistsAsync(appId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.UserCanReadApplicationActionsAsync(appId, ActorUserId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.UserExistsAsync(userId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.GetActionStatusIdByCodeNameAsync("Pending", Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var result = await _sut.CreateAsync(
            new CreateApplicationActionCommand(appId, userId, null), UserActor(), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.StatusNotFound);
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedAction_WhenDataIsValid()
    {
        var appId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var expected = MakeDto(appId, userId, PendingStatusId);

        _repo.ApplicationExistsAsync(appId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.UserCanReadApplicationActionsAsync(appId, ActorUserId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.UserExistsAsync(userId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.CreateAsync(appId, userId, PendingStatusId, null, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.CreateAsync(
            new CreateApplicationActionCommand(appId, userId, null), UserActor(), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task CreateAsync_TrimsComment_BeforePassingToRepo()
    {
        var appId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var expected = MakeDto(appId, userId, PendingStatusId, "Комментарий");

        _repo.ApplicationExistsAsync(appId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.UserCanReadApplicationActionsAsync(appId, ActorUserId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.UserExistsAsync(userId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.CreateAsync(appId, userId, PendingStatusId, "Комментарий", Arg.Any<CancellationToken>())
            .Returns(expected);

        await _sut.CreateAsync(
            new CreateApplicationActionCommand(appId, userId, "  Комментарий  "), UserActor(),
            CancellationToken.None);

        await _repo.Received(1).CreateAsync(appId, userId, PendingStatusId, "Комментарий",
            Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // GetAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_ReturnsNotFound_WhenMissing()
    {
        var id = Guid.NewGuid();
        _repo.GetAsync(id, Arg.Any<CancellationToken>()).Returns((ApplicationActionDto?)null);

        var result = await _sut.GetAsync(id, AdminActor(), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.NotFound);
    }

    [Fact]
    public async Task GetAsync_ReturnsForbidden_WhenNotReader()
    {
        var id = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var dto = MakeDto(appId, Guid.NewGuid(), PendingStatusId, id: id);
        _repo.GetAsync(id, Arg.Any<CancellationToken>()).Returns(dto);
        _repo.UserCanReadApplicationActionsAsync(appId, ActorUserId, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.GetAsync(id, UserActor(), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.Forbidden);
    }

    // -------------------------------------------------------------------------
    // UpdateAsync — валидация и доступ
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_ReturnsValidationError_WhenNoFieldsProvided()
    {
        var id = Guid.NewGuid();
        _repo.GetAsync(id, Arg.Any<CancellationToken>())
            .Returns(MakeDto(Guid.NewGuid(), ActorUserId, PendingStatusId, id: id));

        var result = await _sut.UpdateAsync(id,
            new UpdateApplicationActionCommand(null, null), UserActor(), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.Validation);
        result.Message.Should().Contain("At least one field");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateAsync_ReturnsValidationError_WhenCommentIsBlankString(string comment)
    {
        var id = Guid.NewGuid();
        _repo.GetAsync(id, Arg.Any<CancellationToken>())
            .Returns(MakeDto(Guid.NewGuid(), ActorUserId, PendingStatusId, id: id));

        var result = await _sut.UpdateAsync(id,
            new UpdateApplicationActionCommand(null, comment), UserActor(), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.Validation);
        result.Message.Should().Contain("Comment");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsForbidden_WhenNotResponsibleAndNotAdmin()
    {
        var id = Guid.NewGuid();
        var other = Guid.NewGuid();
        _repo.GetAsync(id, Arg.Any<CancellationToken>())
            .Returns(MakeDto(Guid.NewGuid(), other, PendingStatusId, id: id));

        var result = await _sut.UpdateAsync(id,
            new UpdateApplicationActionCommand(null, "x"), UserActor(), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.Forbidden);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsStatusNotFound_WhenStatusIdDoesNotExist()
    {
        var id = Guid.NewGuid();
        var statusId = Guid.NewGuid();
        _repo.GetAsync(id, Arg.Any<CancellationToken>())
            .Returns(MakeDto(Guid.NewGuid(), ActorUserId, PendingStatusId, id: id));
        _repo.ActionStatusExistsAsync(statusId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.UpdateAsync(id,
            new UpdateApplicationActionCommand(statusId, null), UserActor(), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.StatusNotFound);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenActionDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repo.GetAsync(id, Arg.Any<CancellationToken>()).Returns((ApplicationActionDto?)null);

        var result = await _sut.UpdateAsync(id,
            new UpdateApplicationActionCommand(Guid.NewGuid(), null), UserActor(), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsUpdatedAction_WhenDataIsValid()
    {
        var id = Guid.NewGuid();
        var statusId = Guid.NewGuid();
        var expected = MakeDto(Guid.NewGuid(), ActorUserId, statusId, id: id);

        _repo.GetAsync(id, Arg.Any<CancellationToken>())
            .Returns(MakeDto(Guid.NewGuid(), ActorUserId, PendingStatusId, id: id));
        _repo.ActionStatusExistsAsync(statusId, Arg.Any<CancellationToken>()).Returns(true);
        _repo.UpdateAsync(id, statusId, null, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.UpdateAsync(id,
            new UpdateApplicationActionCommand(statusId, null), UserActor(), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeEquivalentTo(expected);
    }

    // -------------------------------------------------------------------------
    // DeleteAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_ReturnsNotFound_WhenActionDoesNotExist()
    {
        var id = Guid.NewGuid();
        _repo.GetAsync(id, Arg.Any<CancellationToken>()).Returns((ApplicationActionDto?)null);

        var result = await _sut.DeleteAsync(id, UserActor(), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsForbidden_WhenNotResponsibleAndNotAdmin()
    {
        var id = Guid.NewGuid();
        var other = Guid.NewGuid();
        _repo.GetAsync(id, Arg.Any<CancellationToken>())
            .Returns(MakeDto(Guid.NewGuid(), other, PendingStatusId, id: id));

        var result = await _sut.DeleteAsync(id, UserActor(), CancellationToken.None);

        result.Error.Should().Be(ApplicationActionsError.Forbidden);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsOk_WhenActionExistsAndResponsible()
    {
        var id = Guid.NewGuid();
        _repo.GetAsync(id, Arg.Any<CancellationToken>())
            .Returns(MakeDto(Guid.NewGuid(), ActorUserId, PendingStatusId, id: id));
        _repo.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.DeleteAsync(id, UserActor(), CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static ApplicationActionDto MakeDto(Guid applicationId, Guid ResponsibleId, Guid statusId,
        string? comment = null, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), applicationId, ResponsibleId, statusId, "Pending", "На согласовании",
            comment, DateTime.UtcNow, null);
}
