using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.SupervisorRequests;
using AcademicTopicSelectionService.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.SupervisorRequests;

public sealed class SupervisorRequestsServiceTests
{
    private static readonly Guid StudentUserId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid TeacherUserId = Guid.NewGuid();
    private static readonly Guid RequestId = Guid.NewGuid();
    private static readonly Guid PendingStatusId = Guid.NewGuid();
    private static readonly Guid ApprovedStatusId = Guid.NewGuid();

    private readonly ISupervisorRequestsRepository _repo = Substitute.For<ISupervisorRequestsRepository>();
    private readonly IUsersRepository _usersRepo = Substitute.For<IUsersRepository>();
    private readonly IApplicationStatusesRepository _statusesRepo = Substitute.For<IApplicationStatusesRepository>();
    private readonly SupervisorRequestsService _sut;

    public SupervisorRequestsServiceTests()
    {
        _sut = new SupervisorRequestsService(_repo, _usersRepo, _statusesRepo);

        _usersRepo.GetByIdAsync(StudentUserId, Arg.Any<CancellationToken>())
            .Returns(new User
            {
                Id = StudentUserId,
                Role = new UserRole { CodeName = "Student", DisplayName = "Студент" }
            });
        _usersRepo.GetByIdAsync(TeacherUserId, Arg.Any<CancellationToken>())
            .Returns(new User
            {
                Id = TeacherUserId,
                Role = new UserRole { CodeName = "Teacher", DisplayName = "Преподаватель" }
            });

        _repo.GetStudentIdByUserIdAsync(StudentUserId, Arg.Any<CancellationToken>())
            .Returns(StudentId);
        _statusesRepo.GetIdByCodeNameAsync("Pending", Arg.Any<CancellationToken>())
            .Returns(PendingStatusId);
        _statusesRepo.GetIdByCodeNameAsync("ApprovedBySupervisor", Arg.Any<CancellationToken>())
            .Returns(ApprovedStatusId);
    }

    [Fact]
    public async Task CreateAsync_ReturnsConflict_WhenDuplicatePendingExists()
    {
        _repo.HasActiveRequestForTeacherAsync(StudentId, TeacherUserId, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.CreateAsync(
            new CreateSupervisorRequestCommand(TeacherUserId, null),
            StudentUserId,
            CancellationToken.None);

        result.Error.Should().Be(SupervisorRequestsError.Conflict);
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidation_WhenTeacherUserIdIsEmpty()
    {
        var result = await _sut.CreateAsync(
            new CreateSupervisorRequestCommand(Guid.Empty, null),
            StudentUserId,
            CancellationToken.None);

        result.Error.Should().Be(SupervisorRequestsError.Validation);
    }

    [Fact]
    public async Task CreateAsync_ReturnsNotFound_WhenStudentUserMissing()
    {
        _usersRepo.GetByIdAsync(StudentUserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.CreateAsync(
            new CreateSupervisorRequestCommand(TeacherUserId, null),
            StudentUserId,
            CancellationToken.None);

        result.Error.Should().Be(SupervisorRequestsError.NotFound);
    }

    [Fact]
    public async Task CreateAsync_ReturnsForbidden_WhenCallerIsNotStudent()
    {
        _usersRepo.GetByIdAsync(StudentUserId, Arg.Any<CancellationToken>())
            .Returns(new User { Id = StudentUserId, Role = new UserRole { CodeName = "Teacher", DisplayName = "Преподаватель" } });

        var result = await _sut.CreateAsync(
            new CreateSupervisorRequestCommand(TeacherUserId, null),
            StudentUserId,
            CancellationToken.None);

        result.Error.Should().Be(SupervisorRequestsError.Forbidden);
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidation_WhenStudentProfileMissing()
    {
        _repo.GetStudentIdByUserIdAsync(StudentUserId, Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var result = await _sut.CreateAsync(
            new CreateSupervisorRequestCommand(TeacherUserId, null),
            StudentUserId,
            CancellationToken.None);

        result.Error.Should().Be(SupervisorRequestsError.Validation);
    }

    [Fact]
    public async Task CreateAsync_CreatesRequest_WhenValid()
    {
        _repo.HasActiveRequestForTeacherAsync(StudentId, TeacherUserId, Arg.Any<CancellationToken>())
            .Returns(false);
        _repo.AddAsync(Arg.Any<SupervisorRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var request = ci.Arg<SupervisorRequest>();
                request.Id = RequestId;
                return request;
            });
        _repo.GetDetailAsync(RequestId, Arg.Any<CancellationToken>())
            .Returns(new SupervisorRequestDetailDto(
                RequestId,
                StudentId,
                "Student",
                "Test",
                "4411",
                TeacherUserId,
                "Teacher",
                "Test",
                "teacher@test.com",
                new ApplicationStatusRefDto(PendingStatusId, "Pending", "Ожидает"),
                null,
                DateTime.UtcNow,
                null));

        var result = await _sut.CreateAsync(
            new CreateSupervisorRequestCommand(TeacherUserId, null),
            StudentUserId,
            CancellationToken.None);

        result.Error.Should().BeNull();
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveAsync_CancelsOtherPendingRequests()
    {
        _repo.GetByIdWithTrackingAsync(RequestId, Arg.Any<CancellationToken>())
            .Returns(new SupervisorRequest
            {
                Id = RequestId,
                StudentId = StudentId,
                TeacherUserId = TeacherUserId,
                StatusId = PendingStatusId,
                Status = new ApplicationStatus { CodeName = "Pending", DisplayName = "Ожидает" }
            });
        _repo.GetDetailAsync(RequestId, Arg.Any<CancellationToken>())
            .Returns(new SupervisorRequestDetailDto(
                RequestId,
                StudentId,
                "Student",
                "Test",
                "4411",
                TeacherUserId,
                "Teacher",
                "Test",
                "teacher@test.com",
                new ApplicationStatusRefDto(ApprovedStatusId, "ApprovedBySupervisor", "Одобрено"),
                null,
                DateTime.UtcNow,
                null));

        var result = await _sut.ApproveAsync(RequestId, TeacherUserId, CancellationToken.None);

        result.Error.Should().BeNull();
        await _repo.Received(1).CancelAllActiveRequestsExceptAsync(StudentId, RequestId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveAsync_ReturnsForbidden_WhenTeacherDoesNotMatchRequest()
    {
        _repo.GetByIdWithTrackingAsync(RequestId, Arg.Any<CancellationToken>())
            .Returns(new SupervisorRequest
            {
                Id = RequestId,
                StudentId = StudentId,
                TeacherUserId = TeacherUserId,
                Status = new ApplicationStatus { CodeName = "Pending", DisplayName = "Ожидает" }
            });

        var result = await _sut.ApproveAsync(RequestId, Guid.NewGuid(), CancellationToken.None);

        result.Error.Should().Be(SupervisorRequestsError.Forbidden);
    }

    [Fact]
    public async Task ApproveAsync_ReturnsInvalidTransition_WhenStatusIsNotPending()
    {
        _repo.GetByIdWithTrackingAsync(RequestId, Arg.Any<CancellationToken>())
            .Returns(new SupervisorRequest
            {
                Id = RequestId,
                StudentId = StudentId,
                TeacherUserId = TeacherUserId,
                Status = new ApplicationStatus { CodeName = "ApprovedBySupervisor", DisplayName = "Одобрено" }
            });

        var result = await _sut.ApproveAsync(RequestId, TeacherUserId, CancellationToken.None);

        result.Error.Should().Be(SupervisorRequestsError.InvalidTransition);
    }

    [Fact]
    public async Task RejectAsync_ReturnsValidation_WhenCommentMissing()
    {
        var result = await _sut.RejectAsync(
            RequestId,
            new RejectSupervisorRequestCommand(""),
            TeacherUserId,
            CancellationToken.None);

        result.Error.Should().Be(SupervisorRequestsError.Validation);
    }

    [Fact]
    public async Task CancelAsync_ReturnsForbidden_WhenRequestOwnedByAnotherStudent()
    {
        var otherStudentId = Guid.NewGuid();
        _repo.GetStudentIdByUserIdAsync(StudentUserId, Arg.Any<CancellationToken>())
            .Returns(StudentId);
        _repo.GetByIdWithTrackingAsync(RequestId, Arg.Any<CancellationToken>())
            .Returns(new SupervisorRequest
            {
                Id = RequestId,
                StudentId = otherStudentId,
                TeacherUserId = TeacherUserId,
                Status = new ApplicationStatus { CodeName = "Pending", DisplayName = "Ожидает" }
            });

        var result = await _sut.CancelAsync(RequestId, StudentUserId, CancellationToken.None);

        result.Error.Should().Be(SupervisorRequestsError.Forbidden);
    }
}
