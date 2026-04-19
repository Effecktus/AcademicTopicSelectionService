using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Notifications;
using AcademicTopicSelectionService.Application.StudentApplications;
using AcademicTopicSelectionService.Application.Topics;
using AcademicTopicSelectionService.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.StudentApplications;

public sealed class StudentApplicationsServiceTests
{
    private readonly IStudentApplicationsRepository _appRepo = Substitute.For<IStudentApplicationsRepository>();
    private readonly ITopicsRepository _topicRepo = Substitute.For<ITopicsRepository>();
    private readonly ITopicCreatorTypesRepository _topicCreatorTypesRepo = Substitute.For<ITopicCreatorTypesRepository>();
    private readonly ITopicStatusesRepository _topicStatusesRepo = Substitute.For<ITopicStatusesRepository>();
    private readonly IApplicationActionsRepository _actionRepo = Substitute.For<IApplicationActionsRepository>();
    private readonly IUsersRepository _usersRepo = Substitute.For<IUsersRepository>();
    private readonly IApplicationStatusesRepository _appStatusesRepo = Substitute.For<IApplicationStatusesRepository>();
    private readonly INotificationsService _notificationsService = Substitute.For<INotificationsService>();

    private readonly StudentApplicationsService _sut;

    // Class-level counter for GetDetailAsync call tracking within a single test
    private int _getDetailCallCount;

    private static readonly Guid ApplicationId = Guid.NewGuid();
    private static readonly Guid StudentUserId = Guid.NewGuid();
    private static readonly Guid StudentProfileId = Guid.NewGuid();
    private static readonly Guid SupervisorUserId = Guid.NewGuid();
    private static readonly Guid DeptHeadUserId = Guid.NewGuid();
    private static readonly Guid TopicId = Guid.NewGuid();
    private static readonly Guid SupervisorRequestId = Guid.NewGuid();
    private static readonly Guid DepartmentId = Guid.NewGuid();
    private static readonly Guid PendingStatusId = Guid.NewGuid();
    private static readonly Guid ApprovedBySupStatusId = Guid.NewGuid();
    private static readonly Guid PendingDeptHeadStatusId = Guid.NewGuid();
    private static readonly Guid ApprovedByDeptHeadStatusId = Guid.NewGuid();
    private static readonly Guid RejectedBySupStatusId = Guid.NewGuid();
    private static readonly Guid RejectedByDeptHeadStatusId = Guid.NewGuid();
    private static readonly Guid CancelledStatusId = Guid.NewGuid();

    public StudentApplicationsServiceTests()
    {
        _getDetailCallCount = 0;
        _sut = new StudentApplicationsService(
            _appRepo, _topicRepo, _topicCreatorTypesRepo, _topicStatusesRepo, _actionRepo, _usersRepo, _appStatusesRepo,
            _notificationsService);

        // Setup status IDs
        _appStatusesRepo.GetIdByCodeNameAsync("Pending", Arg.Any<CancellationToken>()).Returns(PendingStatusId);
        _appStatusesRepo.GetIdByCodeNameAsync("ApprovedBySupervisor", Arg.Any<CancellationToken>()).Returns(ApprovedBySupStatusId);
        _appStatusesRepo.GetIdByCodeNameAsync("RejectedBySupervisor", Arg.Any<CancellationToken>()).Returns(RejectedBySupStatusId);
        _appStatusesRepo.GetIdByCodeNameAsync("PendingDepartmentHead", Arg.Any<CancellationToken>()).Returns(PendingDeptHeadStatusId);
        _appStatusesRepo.GetIdByCodeNameAsync("ApprovedByDepartmentHead", Arg.Any<CancellationToken>()).Returns(ApprovedByDeptHeadStatusId);
        _appStatusesRepo.GetIdByCodeNameAsync("RejectedByDepartmentHead", Arg.Any<CancellationToken>()).Returns(RejectedByDeptHeadStatusId);
        _appStatusesRepo.GetIdByCodeNameAsync("Cancelled", Arg.Any<CancellationToken>()).Returns(CancelledStatusId);

        _actionRepo.GetActionStatusIdByCodeNameAsync("Pending", Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());
        _actionRepo.GetActionStatusIdByCodeNameAsync("Approved", Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());
        _actionRepo.GetActionStatusIdByCodeNameAsync("Rejected", Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());
        _actionRepo.GetActionStatusIdByCodeNameAsync("Cancelled", Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());

        _appRepo.GetStudentIdByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(StudentProfileId);
        _appRepo.GetApprovedSupervisorRequestAsync(SupervisorRequestId, StudentProfileId, Arg.Any<CancellationToken>())
            .Returns(new SupervisorRequest
            {
                Id = SupervisorRequestId,
                StudentId = StudentProfileId,
                TeacherUserId = SupervisorUserId,
                Status = new ApplicationStatus { CodeName = "ApprovedBySupervisor", DisplayName = "Одобрено" }
            });
        _appRepo.GetTeacherByUserIdAsync(SupervisorUserId, Arg.Any<CancellationToken>()).Returns((Teacher?)null);

        // Default: no active applications
        _appRepo.StudentHasActiveApplicationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        _appRepo.HasActiveApplicationOnTopicAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        // Default: topics exist and are active
        _topicRepo.ExistsByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _topicRepo.IsActiveByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _topicRepo.GetAsync(TopicId, Arg.Any<CancellationToken>()).Returns(MakeTopicDto(TopicId));
        _topicCreatorTypesRepo.GetIdByCodeNameAsync("Student", Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());
        _topicStatusesRepo.GetIdByCodeNameAsync("Active", Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());
    }

    // -------------------------------------------------------------------------
    // CreateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenUserNotFound()
    {
        _usersRepo.GetByIdAsync(StudentUserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.CreateAsync(
            new CreateApplicationCommand(TopicId, SupervisorRequestId), StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.NotFound);
    }

    [Fact]
    public async Task CreateAsync_ReturnsNotFound_WhenTopicNotFound()
    {
        _usersRepo.GetByIdAsync(StudentUserId, Arg.Any<CancellationToken>()).Returns(MakeStudentUser());
        _topicRepo.ExistsByIdAsync(TopicId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.CreateAsync(
            new CreateApplicationCommand(TopicId, SupervisorRequestId), StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.NotFound);
        result.Message.Should().Contain("Topic");
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenTopicIsNotActive()
    {
        _usersRepo.GetByIdAsync(StudentUserId, Arg.Any<CancellationToken>()).Returns(MakeStudentUser());
        _topicRepo.ExistsByIdAsync(TopicId, Arg.Any<CancellationToken>()).Returns(true);
        _topicRepo.IsActiveByIdAsync(TopicId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.CreateAsync(
            new CreateApplicationCommand(TopicId, SupervisorRequestId), StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Validation);
        result.Message.Should().Contain("not active");
        await _appRepo.DidNotReceive().AddAsync(Arg.Any<StudentApplication>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenStudentProfileNotFound()
    {
        _usersRepo.GetByIdAsync(StudentUserId, Arg.Any<CancellationToken>()).Returns(MakeStudentUser());
        _topicRepo.ExistsByIdAsync(TopicId, Arg.Any<CancellationToken>()).Returns(true);
        _appRepo.GetStudentIdByUserIdAsync(StudentUserId, Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var result = await _sut.CreateAsync(
            new CreateApplicationCommand(TopicId, SupervisorRequestId), StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Validation);
        result.Message.Should().Contain("Student profile");
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenNeitherTopicNorProposalProvided()
    {
        _usersRepo.GetByIdAsync(StudentUserId, Arg.Any<CancellationToken>()).Returns(MakeStudentUser());

        var result = await _sut.CreateAsync(
            new CreateApplicationCommand(null, SupervisorRequestId, null, null), StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Validation);
        result.Message.Should().Contain("either TopicId or ProposedTitle");
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenBothTopicAndProposalProvided()
    {
        _usersRepo.GetByIdAsync(StudentUserId, Arg.Any<CancellationToken>()).Returns(MakeStudentUser());

        var result = await _sut.CreateAsync(
            new CreateApplicationCommand(TopicId, SupervisorRequestId, "Новая тема", "Описание"), StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Validation);
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenProposedTitleIsWhitespace()
    {
        _usersRepo.GetByIdAsync(StudentUserId, Arg.Any<CancellationToken>()).Returns(MakeStudentUser());

        var result = await _sut.CreateAsync(
            new CreateApplicationCommand(null, SupervisorRequestId, "   ", "Описание"), StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Validation);
        result.Message.Should().Contain("either TopicId or ProposedTitle");
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenProposedTitleTooLong()
    {
        _usersRepo.GetByIdAsync(StudentUserId, Arg.Any<CancellationToken>()).Returns(MakeStudentUser());
        var tooLongTitle = new string('A', 501);

        var result = await _sut.CreateAsync(
            new CreateApplicationCommand(null, SupervisorRequestId, tooLongTitle, "Описание"), StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Validation);
        result.Message.Should().Contain("<= 500");
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenApprovedSupervisorRequestNotFound()
    {
        _usersRepo.GetByIdAsync(StudentUserId, Arg.Any<CancellationToken>()).Returns(MakeStudentUser());
        _appRepo.GetApprovedSupervisorRequestAsync(SupervisorRequestId, StudentProfileId, Arg.Any<CancellationToken>())
            .Returns((SupervisorRequest?)null);

        var result = await _sut.CreateAsync(
            new CreateApplicationCommand(TopicId, SupervisorRequestId), StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Validation);
        result.Message.Should().Contain("Approved supervisor request");
    }

    [Fact]
    public async Task CreateAsync_ReturnsForbidden_WhenUserIsNotStudent()
    {
        var teacherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "teacher@test.com",
            FirstName = "Teacher",
            LastName = "Test",
            RoleId = Guid.NewGuid(),
            IsActive = true,
            Role = new UserRole { Id = Guid.NewGuid(), CodeName = "Teacher", DisplayName = "Преподаватель" }
        };
        _usersRepo.GetByIdAsync(teacherUser.Id, Arg.Any<CancellationToken>()).Returns(teacherUser);

        var result = await _sut.CreateAsync(
            new CreateApplicationCommand(TopicId, SupervisorRequestId), teacherUser.Id, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Forbidden);
        result.Message.Should().Contain("Only students");
        await _topicRepo.DidNotReceive().ExistsByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsForbidden_WhenUserIsAdmin()
    {
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@test.com",
            FirstName = "Admin",
            LastName = "Test",
            RoleId = Guid.NewGuid(),
            IsActive = true,
            Role = new UserRole { Id = Guid.NewGuid(), CodeName = "Admin", DisplayName = "Администратор" }
        };
        _usersRepo.GetByIdAsync(adminUser.Id, Arg.Any<CancellationToken>()).Returns(adminUser);

        var result = await _sut.CreateAsync(
            new CreateApplicationCommand(TopicId, SupervisorRequestId), adminUser.Id, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Forbidden);
        result.Message.Should().Contain("Only students");
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidationError_WhenStudentHasActiveApplication()
    {
        _usersRepo.GetByIdAsync(StudentUserId, Arg.Any<CancellationToken>()).Returns(MakeStudentUser());
        _topicRepo.ExistsByIdAsync(TopicId, Arg.Any<CancellationToken>()).Returns(true);
        _appRepo.StudentHasActiveApplicationAsync(StudentProfileId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(
            new CreateApplicationCommand(TopicId, SupervisorRequestId), StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Validation);
        result.Message.Should().Contain("already has an active application");
    }

    [Fact]
    public async Task CreateAsync_ReturnsConflict_WhenTopicAlreadyTaken()
    {
        _usersRepo.GetByIdAsync(StudentUserId, Arg.Any<CancellationToken>()).Returns(MakeStudentUser());
        _topicRepo.ExistsByIdAsync(TopicId, Arg.Any<CancellationToken>()).Returns(true);
        _appRepo.HasActiveApplicationOnTopicAsync(TopicId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(
            new CreateApplicationCommand(TopicId, SupervisorRequestId), StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Conflict);
        result.Message.Should().Contain("already taken");
    }

    [Fact]
    public async Task CreateAsync_CreatesApplication_WhenValid()
    {
        var createdApplicationId = Guid.Empty;
        _usersRepo.GetByIdAsync(StudentUserId, Arg.Any<CancellationToken>()).Returns(MakeStudentUser());
        _topicRepo.ExistsByIdAsync(TopicId, Arg.Any<CancellationToken>()).Returns(true);
        _appRepo.AddAsync(Arg.Any<StudentApplication>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var app = ci.Arg<StudentApplication>();
                createdApplicationId = app.Id;
                return app;
            });
        _appRepo.GetDetailAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => MakeDetailDto(ci.Arg<Guid>(), "Pending"));

        var result = await _sut.CreateAsync(
            new CreateApplicationCommand(TopicId, SupervisorRequestId), StudentUserId, CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(createdApplicationId);
        await _appRepo.Received(1).AddAsync(
            Arg.Is<StudentApplication>(a => a.StudentId == StudentProfileId && a.TopicId == TopicId && a.StatusId == PendingStatusId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_CreatesFirstAction()
    {
        var createdApplicationId = Guid.Empty;
        _usersRepo.GetByIdAsync(StudentUserId, Arg.Any<CancellationToken>()).Returns(MakeStudentUser());
        _topicRepo.ExistsByIdAsync(TopicId, Arg.Any<CancellationToken>()).Returns(true);
        _appRepo.AddAsync(Arg.Any<StudentApplication>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var app = ci.Arg<StudentApplication>();
                createdApplicationId = app.Id;
                return app;
            });
        _appRepo.GetDetailAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => MakeDetailDto(ci.Arg<Guid>(), "Pending"));

        await _sut.CreateAsync(
            new CreateApplicationCommand(TopicId, SupervisorRequestId), StudentUserId, CancellationToken.None);

        _actionRepo.Received(1).Enqueue(
            createdApplicationId, StudentUserId, Arg.Any<Guid>(), null);
    }

    [Fact]
    public async Task CreateAsync_SendsNotificationToSupervisor_WhenValid()
    {
        _usersRepo.GetByIdAsync(StudentUserId, Arg.Any<CancellationToken>()).Returns(MakeStudentUser());
        _topicRepo.ExistsByIdAsync(TopicId, Arg.Any<CancellationToken>()).Returns(true);
        _appRepo.AddAsync(Arg.Any<StudentApplication>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var app = ci.Arg<StudentApplication>();
                return app;
            });
        _appRepo.GetDetailAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => MakeDetailDto(ci.Arg<Guid>(), "Pending"));

        _notificationsService.CreateAsync(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = SupervisorUserId,
                Title = "Новая заявка на тему ВКР",
                Content = "Тестовое уведомление",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

        var result = await _sut.CreateAsync(
            new CreateApplicationCommand(TopicId, SupervisorRequestId), StudentUserId, CancellationToken.None);

        result.Error.Should().BeNull();
        await _notificationsService.Received(1).CreateAsync(
            Arg.Is<CreateNotificationCommand>(c =>
                c.UserId == SupervisorUserId &&
                c.TypeCodeName == "ApplicationSubmittedToSupervisor" &&
                c.Title == "Новая заявка на тему ВКР" &&
                c.Content.Contains("Студент")),
            Arg.Any<CancellationToken>());
        await _notificationsService.Received(1).EnqueueEmailAsync(
            SupervisorUserId,
            "Новая заявка на тему ВКР",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_CreatesTopic_WhenStudentProposesNewOne()
    {
        _usersRepo.GetByIdAsync(StudentUserId, Arg.Any<CancellationToken>()).Returns(MakeStudentUser());
        _topicRepo.AddAsync(Arg.Any<Topic>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var topic = ci.Arg<Topic>();
                topic.Id = TopicId;
                return topic;
            });
        _appRepo.AddAsync(Arg.Any<StudentApplication>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<StudentApplication>());
        _appRepo.GetDetailAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => MakeDetailDto(ci.Arg<Guid>(), "Pending"));

        var result = await _sut.CreateAsync(
            new CreateApplicationCommand(null, SupervisorRequestId, "Предложенная тема", "Описание"), StudentUserId, CancellationToken.None);

        result.Error.Should().BeNull();
        await _topicRepo.Received(1).AddAsync(
            Arg.Is<Topic>(t => t.Title == "Предложенная тема" && t.CreatedBy == StudentUserId),
            Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // ApproveBySupervisorAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ApproveBySupervisorAsync_ReturnsNotFound_WhenApplicationNotFound()
    {
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>()).Returns((StudentApplicationDetailDto?)null);

        var result = await _sut.ApproveBySupervisorAsync(
            ApplicationId, new ApproveBySupervisorCommand(null), SupervisorUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.NotFound);
    }

    [Fact]
    public async Task ApproveBySupervisorAsync_ReturnsForbidden_WhenCallerIsNotSupervisor()
    {
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeDetailDto(ApplicationId, "Pending", supervisorUserId: SupervisorUserId));

        var result = await _sut.ApproveBySupervisorAsync(
            ApplicationId, new ApproveBySupervisorCommand(null), OtherUserId(), CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Forbidden);
    }

    [Fact]
    public async Task ApproveBySupervisorAsync_ReturnsInvalidTransition_WhenStatusIsNotPending()
    {
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeDetailDto(ApplicationId, "ApprovedBySupervisor", supervisorUserId: SupervisorUserId));

        var result = await _sut.ApproveBySupervisorAsync(
            ApplicationId, new ApproveBySupervisorCommand(null), SupervisorUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.InvalidTransition);
    }

    [Fact]
    public async Task ApproveBySupervisorAsync_TransitionsToApprovedBySupervisor()
    {
        _getDetailCallCount = 0;
        var pendingDetail = MakeDetailDto(ApplicationId, "Pending", supervisorUserId: SupervisorUserId);
        var approvedDetail = MakeDetailDto(ApplicationId, "ApprovedBySupervisor", supervisorUserId: SupervisorUserId);

        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _getDetailCallCount++;
                // 1=VerifySupervisor, 2=Transition status check, 3=return DTO
                return _getDetailCallCount <= 2 ? pendingDetail : approvedDetail;
            });
        _appRepo.GetByIdWithTrackingAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeApplicationEntity());

        var result = await _sut.ApproveBySupervisorAsync(
            ApplicationId, new ApproveBySupervisorCommand("Good work"), SupervisorUserId, CancellationToken.None);

        result.Error.Should().BeNull();
        await _appRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveBySupervisorAsync_SendsNotificationToStudent_WhenValid()
    {
        _getDetailCallCount = 0;
        var pendingDetail = MakeDetailDto(ApplicationId, "Pending", supervisorUserId: SupervisorUserId);
        var approvedDetail = MakeDetailDto(ApplicationId, "ApprovedBySupervisor", supervisorUserId: SupervisorUserId);

        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _getDetailCallCount++;
                return _getDetailCallCount <= 2 ? pendingDetail : approvedDetail;
            });
        _appRepo.GetByIdWithTrackingAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeApplicationEntity());

        _notificationsService.CreateAsync(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = StudentUserId,
                Title = "Заявка одобрена научным руководителем",
                Content = "Научный руководитель одобрил вашу заявку.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

        var result = await _sut.ApproveBySupervisorAsync(
            ApplicationId, new ApproveBySupervisorCommand("Отличная тема"), SupervisorUserId, CancellationToken.None);

        result.Error.Should().BeNull();
        await _notificationsService.Received(1).CreateAsync(
            Arg.Is<CreateNotificationCommand>(c =>
                c.UserId == StudentUserId &&
                c.TypeCodeName == "ApplicationStatusChanged" &&
                c.Title == "Заявка одобрена научным руководителем"),
            Arg.Any<CancellationToken>());
        await _notificationsService.Received(1).EnqueueEmailAsync(
            StudentUserId,
            "Заявка одобрена научным руководителем",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // RejectBySupervisorAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RejectBySupervisorAsync_ReturnsValidationError_WhenCommentIsEmpty()
    {
        var result = await _sut.RejectBySupervisorAsync(
            ApplicationId, new RejectBySupervisorCommand(""), SupervisorUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Validation);
        result.Message.Should().Contain("Rejection reason");
    }

    [Fact]
    public async Task RejectBySupervisorAsync_TransitionsToRejectedBySupervisor()
    {
        _getDetailCallCount = 0;
        var pendingDetail = MakeDetailDto(ApplicationId, "Pending", supervisorUserId: SupervisorUserId);
        var rejectedDetail = MakeDetailDto(ApplicationId, "RejectedBySupervisor", supervisorUserId: SupervisorUserId);

        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _getDetailCallCount++;
                return _getDetailCallCount <= 2 ? pendingDetail : rejectedDetail;
            });
        _appRepo.GetByIdWithTrackingAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeApplicationEntity());

        var result = await _sut.RejectBySupervisorAsync(
            ApplicationId, new RejectBySupervisorCommand("Not suitable"), SupervisorUserId, CancellationToken.None);

        result.Error.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // RejectBySupervisorAsync — дополнительные проверки
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RejectBySupervisorAsync_ReturnsNotFound_WhenApplicationNotFound()
    {
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>()).Returns((StudentApplicationDetailDto?)null);

        var result = await _sut.RejectBySupervisorAsync(
            ApplicationId, new RejectBySupervisorCommand("Не подходит"), SupervisorUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.NotFound);
    }

    [Fact]
    public async Task RejectBySupervisorAsync_ReturnsForbidden_WhenCallerIsNotSupervisor()
    {
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeDetailDto(ApplicationId, "Pending", supervisorUserId: SupervisorUserId));

        var result = await _sut.RejectBySupervisorAsync(
            ApplicationId, new RejectBySupervisorCommand("Не подходит"), OtherUserId(), CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Forbidden);
    }

    [Fact]
    public async Task RejectBySupervisorAsync_ReturnsInvalidTransition_WhenStatusIsNotPending()
    {
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeDetailDto(ApplicationId, "RejectedBySupervisor", supervisorUserId: SupervisorUserId));

        var result = await _sut.RejectBySupervisorAsync(
            ApplicationId, new RejectBySupervisorCommand("Не подходит"), SupervisorUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.InvalidTransition);
    }

    [Fact]
    public async Task RejectBySupervisorAsync_ReturnsValidationError_WhenCommentIsNull()
    {
        var result = await _sut.RejectBySupervisorAsync(
            ApplicationId, new RejectBySupervisorCommand(null!), SupervisorUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Validation);
        result.Message.Should().Contain("Rejection reason");
    }

    [Fact]
    public async Task RejectBySupervisorAsync_ReturnsValidationError_WhenCommentIsWhitespace()
    {
        var result = await _sut.RejectBySupervisorAsync(
            ApplicationId, new RejectBySupervisorCommand("   "), SupervisorUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Validation);
    }

    // -------------------------------------------------------------------------
    // SubmitToDepartmentHeadAsync — дополнительные проверки
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SubmitToDepartmentHeadAsync_ReturnsNotFound_WhenApplicationNotFound()
    {
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>()).Returns((StudentApplicationDetailDto?)null);

        var result = await _sut.SubmitToDepartmentHeadAsync(
            ApplicationId, new SubmitToDepartmentHeadCommand(null), SupervisorUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.NotFound);
    }

    [Fact]
    public async Task SubmitToDepartmentHeadAsync_ReturnsForbidden_WhenCallerIsNotSupervisor()
    {
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeDetailDto(ApplicationId, "ApprovedBySupervisor", supervisorUserId: SupervisorUserId));

        var result = await _sut.SubmitToDepartmentHeadAsync(
            ApplicationId, new SubmitToDepartmentHeadCommand(null), OtherUserId(), CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Forbidden);
    }

    [Fact]
    public async Task SubmitToDepartmentHeadAsync_ReturnsInvalidTransition_WhenStatusIsNotApprovedBySupervisor()
    {
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeDetailDto(ApplicationId, "Pending", supervisorUserId: SupervisorUserId));
        _usersRepo.GetByIdAsync(SupervisorUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithDepartment(SupervisorUserId, DepartmentId));

        var result = await _sut.SubmitToDepartmentHeadAsync(
            ApplicationId, new SubmitToDepartmentHeadCommand(null), SupervisorUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.InvalidTransition);
    }

    // -------------------------------------------------------------------------
    // ApproveByDepartmentHeadAsync — дополнительные проверки
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ApproveByDepartmentHeadAsync_ReturnsNotFound_WhenUserNotFound()
    {
        _usersRepo.GetByIdAsync(DeptHeadUserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.ApproveByDepartmentHeadAsync(
            ApplicationId, new ApproveByDepartmentHeadCommand(null), DeptHeadUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.NotFound);
    }

    [Fact]
    public async Task ApproveByDepartmentHeadAsync_ReturnsInvalidTransition_WhenStatusIsNotPendingDepartmentHead()
    {
        _usersRepo.GetByIdAsync(DeptHeadUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithRole(DeptHeadUserId, "DepartmentHead", DepartmentId));
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeDetailDto(ApplicationId, "PendingDepartmentHead", supervisorUserId: SupervisorUserId, supervisorDeptId: DepartmentId));
        _usersRepo.GetByIdAsync(SupervisorUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithDepartment(SupervisorUserId, DepartmentId));
        // Need to set up the Transition status check to fail
        _getDetailCallCount = 0;
        var detail = MakeDetailDto(ApplicationId, "ApprovedByDepartmentHead", supervisorUserId: SupervisorUserId, supervisorDeptId: DepartmentId);
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(_ => { _getDetailCallCount++; return detail; });

        var result = await _sut.ApproveByDepartmentHeadAsync(
            ApplicationId, new ApproveByDepartmentHeadCommand(null), DeptHeadUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.InvalidTransition);
    }

    [Fact]
    public async Task ApproveByDepartmentHeadAsync_AllowsApproval_WhenTeacherProfileIsNull()
    {
        _getDetailCallCount = 0;
        var pendingDetail = MakeDetailDto(ApplicationId, "PendingDepartmentHead", supervisorUserId: SupervisorUserId, supervisorDeptId: DepartmentId);
        var approvedDetail = MakeDetailDto(ApplicationId, "ApprovedByDepartmentHead", supervisorUserId: SupervisorUserId);

        _usersRepo.GetByIdAsync(DeptHeadUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithRole(DeptHeadUserId, "DepartmentHead", DepartmentId));
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(_ => { _getDetailCallCount++; return _getDetailCallCount <= 3 ? pendingDetail : approvedDetail; });
        _usersRepo.GetByIdAsync(SupervisorUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithDepartment(SupervisorUserId, DepartmentId));
        _appRepo.CountOccupiedSlotsBySupervisorAsync(SupervisorUserId, Arg.Any<CancellationToken>()).Returns(0);
        _appRepo.GetTeacherByUserIdAsync(SupervisorUserId, Arg.Any<CancellationToken>()).Returns((Teacher?)null);
        _appRepo.GetByIdWithTrackingAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeApplicationEntity());

        var result = await _sut.ApproveByDepartmentHeadAsync(
            ApplicationId, new ApproveByDepartmentHeadCommand("Approved"), DeptHeadUserId, CancellationToken.None);

        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task ApproveByDepartmentHeadAsync_AllowsApproval_WhenWithinLimit()
    {
        _getDetailCallCount = 0;
        var pendingDetail = MakeDetailDto(ApplicationId, "PendingDepartmentHead", supervisorUserId: SupervisorUserId, supervisorDeptId: DepartmentId);
        var approvedDetail = MakeDetailDto(ApplicationId, "ApprovedByDepartmentHead", supervisorUserId: SupervisorUserId);

        _usersRepo.GetByIdAsync(DeptHeadUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithRole(DeptHeadUserId, "DepartmentHead", DepartmentId));
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(_ => { _getDetailCallCount++; return _getDetailCallCount <= 3 ? pendingDetail : approvedDetail; });
        _usersRepo.GetByIdAsync(SupervisorUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithDepartment(SupervisorUserId, DepartmentId));
        _appRepo.CountOccupiedSlotsBySupervisorAsync(SupervisorUserId, Arg.Any<CancellationToken>()).Returns(2);
        _appRepo.GetTeacherByUserIdAsync(SupervisorUserId, Arg.Any<CancellationToken>())
            .Returns(new Teacher { UserId = SupervisorUserId, MaxStudentsLimit = 3 });
        _appRepo.GetByIdWithTrackingAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeApplicationEntity());

        var result = await _sut.ApproveByDepartmentHeadAsync(
            ApplicationId, new ApproveByDepartmentHeadCommand(null), DeptHeadUserId, CancellationToken.None);

        result.Error.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // RejectByDepartmentHeadAsync — дополнительные проверки
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RejectByDepartmentHeadAsync_ReturnsForbidden_WhenCallerIsNotDepartmentHead()
    {
        _usersRepo.GetByIdAsync(DeptHeadUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithRole(DeptHeadUserId, "Teacher", DepartmentId));

        var result = await _sut.RejectByDepartmentHeadAsync(
            ApplicationId, new RejectByDepartmentHeadCommand("Reason"), DeptHeadUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Forbidden);
    }

    [Fact]
    public async Task RejectByDepartmentHeadAsync_ReturnsForbidden_WhenDepartmentMismatch()
    {
        _usersRepo.GetByIdAsync(DeptHeadUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithRole(DeptHeadUserId, "DepartmentHead", Guid.NewGuid()));
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeDetailDto(ApplicationId, "PendingDepartmentHead", supervisorUserId: SupervisorUserId, supervisorDeptId: DepartmentId));
        _usersRepo.GetByIdAsync(SupervisorUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithDepartment(SupervisorUserId, DepartmentId));

        var result = await _sut.RejectByDepartmentHeadAsync(
            ApplicationId, new RejectByDepartmentHeadCommand("Reason"), DeptHeadUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Forbidden);
    }

    [Fact]
    public async Task RejectByDepartmentHeadAsync_ReturnsNotFound_WhenApplicationNotFound()
    {
        _usersRepo.GetByIdAsync(DeptHeadUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithRole(DeptHeadUserId, "DepartmentHead", DepartmentId));
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>()).Returns((StudentApplicationDetailDto?)null);

        var result = await _sut.RejectByDepartmentHeadAsync(
            ApplicationId, new RejectByDepartmentHeadCommand("Reason"), DeptHeadUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.NotFound);
    }

    [Fact]
    public async Task RejectByDepartmentHeadAsync_ReturnsInvalidTransition_WhenStatusIsNotPendingDepartmentHead()
    {
        _usersRepo.GetByIdAsync(DeptHeadUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithRole(DeptHeadUserId, "DepartmentHead", DepartmentId));
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeDetailDto(ApplicationId, "Pending", supervisorUserId: SupervisorUserId, supervisorDeptId: DepartmentId));
        _usersRepo.GetByIdAsync(SupervisorUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithDepartment(SupervisorUserId, DepartmentId));

        var result = await _sut.RejectByDepartmentHeadAsync(
            ApplicationId, new RejectByDepartmentHeadCommand("Reason"), DeptHeadUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.InvalidTransition);
    }

    [Fact]
    public async Task RejectByDepartmentHeadAsync_ReturnsValidationError_WhenCommentIsNull()
    {
        _usersRepo.GetByIdAsync(DeptHeadUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithRole(DeptHeadUserId, "DepartmentHead", DepartmentId));

        var result = await _sut.RejectByDepartmentHeadAsync(
            ApplicationId, new RejectByDepartmentHeadCommand(null!), DeptHeadUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Validation);
        result.Message.Should().Contain("Rejection reason");
    }

    // -------------------------------------------------------------------------
    // CancelAsync — дополнительные проверки
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CancelAsync_ReturnsNotFound_WhenStudentProfileNotFound()
    {
        _appRepo.GetStudentIdByUserIdAsync(StudentUserId, Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var result = await _sut.CancelAsync(ApplicationId, StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.NotFound);
        result.Message.Should().Contain("Student profile");
    }

    [Fact]
    public async Task CancelAsync_ReturnsInvalidTransition_WhenApprovedByDepartmentHead()
    {
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeDetailDto(ApplicationId, "ApprovedByDepartmentHead", studentId: StudentProfileId));

        var result = await _sut.CancelAsync(ApplicationId, StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.InvalidTransition);
    }

    [Fact]
    public async Task CancelAsync_ReturnsInvalidTransition_WhenRejectedByDepartmentHead()
    {
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeDetailDto(ApplicationId, "RejectedByDepartmentHead", studentId: StudentProfileId));

        var result = await _sut.CancelAsync(ApplicationId, StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.InvalidTransition);
    }

    [Fact]
    public async Task CancelAsync_ReturnsInvalidTransition_WhenAlreadyCancelled()
    {
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeDetailDto(ApplicationId, "Cancelled", studentId: StudentProfileId));

        var result = await _sut.CancelAsync(ApplicationId, StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.InvalidTransition);
    }

    // -------------------------------------------------------------------------
    // SubmitToDepartmentHeadAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SubmitToDepartmentHeadAsync_ReturnsValidationError_WhenSupervisorHasNoDepartment()
    {
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeDetailDto(ApplicationId, "ApprovedBySupervisor", supervisorUserId: SupervisorUserId));
        _usersRepo.GetByIdAsync(SupervisorUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithNoDepartment(SupervisorUserId));

        var result = await _sut.SubmitToDepartmentHeadAsync(
            ApplicationId, new SubmitToDepartmentHeadCommand(null), SupervisorUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Validation);
        result.Message.Should().Contain("no department");
    }

    [Fact]
    public async Task SubmitToDepartmentHeadAsync_TransitionsToPendingDepartmentHead()
    {
        _getDetailCallCount = 0;
        var approvedDetail = MakeDetailDto(ApplicationId, "ApprovedBySupervisor", supervisorUserId: SupervisorUserId);
        var pendingDeptHeadDetail = MakeDetailDto(ApplicationId, "PendingDepartmentHead", supervisorUserId: SupervisorUserId);

        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _getDetailCallCount++;
                // 1=VerifySupervisor, 2=dept head null check, 3=Transition status check, 4=return DTO
                return _getDetailCallCount <= 3 ? approvedDetail : pendingDeptHeadDetail;
            });
        _usersRepo.GetByIdAsync(SupervisorUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithDepartment(SupervisorUserId, DepartmentId));
        _appRepo.GetByIdWithTrackingAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeApplicationEntity());

        var result = await _sut.SubmitToDepartmentHeadAsync(
            ApplicationId, new SubmitToDepartmentHeadCommand("Ready for review"), SupervisorUserId, CancellationToken.None);

        result.Error.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // ApproveByDepartmentHeadAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ApproveByDepartmentHeadAsync_ReturnsForbidden_WhenCallerIsNotDepartmentHead()
    {
        _usersRepo.GetByIdAsync(DeptHeadUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithRole(DeptHeadUserId, "Teacher", DepartmentId));

        var result = await _sut.ApproveByDepartmentHeadAsync(
            ApplicationId, new ApproveByDepartmentHeadCommand(null), DeptHeadUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Forbidden);
    }

    [Fact]
    public async Task ApproveByDepartmentHeadAsync_ReturnsForbidden_WhenDepartmentMismatch()
    {
        _usersRepo.GetByIdAsync(DeptHeadUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithRole(DeptHeadUserId, "DepartmentHead", Guid.NewGuid())); // different dept
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeDetailDto(ApplicationId, "PendingDepartmentHead", supervisorUserId: SupervisorUserId, supervisorDeptId: DepartmentId));
        _usersRepo.GetByIdAsync(SupervisorUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithDepartment(SupervisorUserId, DepartmentId));

        var result = await _sut.ApproveByDepartmentHeadAsync(
            ApplicationId, new ApproveByDepartmentHeadCommand(null), DeptHeadUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Forbidden);
    }

    [Fact]
    public async Task ApproveByDepartmentHeadAsync_ReturnsSupervisorLimitExceeded()
    {
        _usersRepo.GetByIdAsync(DeptHeadUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithRole(DeptHeadUserId, "DepartmentHead", DepartmentId));
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeDetailDto(ApplicationId, "PendingDepartmentHead", supervisorUserId: SupervisorUserId, supervisorDeptId: DepartmentId));
        _usersRepo.GetByIdAsync(SupervisorUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithDepartment(SupervisorUserId, DepartmentId));
        _appRepo.CountOccupiedSlotsBySupervisorAsync(SupervisorUserId, Arg.Any<CancellationToken>()).Returns(3);
        _appRepo.GetTeacherByUserIdAsync(SupervisorUserId, Arg.Any<CancellationToken>())
            .Returns(new Teacher { UserId = SupervisorUserId, MaxStudentsLimit = 3 });

        var result = await _sut.ApproveByDepartmentHeadAsync(
            ApplicationId, new ApproveByDepartmentHeadCommand(null), DeptHeadUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.SupervisorLimitExceeded);
    }

    [Fact]
    public async Task ApproveByDepartmentHeadAsync_TransitionsToApprovedByDepartmentHead()
    {
        _getDetailCallCount = 0;
        var pendingDetail = MakeDetailDto(ApplicationId, "PendingDepartmentHead", supervisorUserId: SupervisorUserId, supervisorDeptId: DepartmentId);
        var approvedDetail = MakeDetailDto(ApplicationId, "ApprovedByDepartmentHead", supervisorUserId: SupervisorUserId);

        _usersRepo.GetByIdAsync(DeptHeadUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithRole(DeptHeadUserId, "DepartmentHead", DepartmentId));
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _getDetailCallCount++;
                // 1=VerifyDeptHead, 2=limit check, 3=Transition status check, 4=return DTO
                return _getDetailCallCount <= 3 ? pendingDetail : approvedDetail;
            });
        _usersRepo.GetByIdAsync(SupervisorUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithDepartment(SupervisorUserId, DepartmentId));
        _appRepo.CountOccupiedSlotsBySupervisorAsync(SupervisorUserId, Arg.Any<CancellationToken>()).Returns(1);
        _appRepo.GetByIdWithTrackingAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeApplicationEntity());

        var result = await _sut.ApproveByDepartmentHeadAsync(
            ApplicationId, new ApproveByDepartmentHeadCommand("Approved"), DeptHeadUserId, CancellationToken.None);

        result.Error.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // RejectByDepartmentHeadAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RejectByDepartmentHeadAsync_ReturnsValidationError_WhenCommentIsEmpty()
    {
        _usersRepo.GetByIdAsync(DeptHeadUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithRole(DeptHeadUserId, "DepartmentHead", DepartmentId));

        var result = await _sut.RejectByDepartmentHeadAsync(
            ApplicationId, new RejectByDepartmentHeadCommand(""), DeptHeadUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Validation);
        result.Message.Should().Contain("Rejection reason");
    }

    [Fact]
    public async Task RejectByDepartmentHeadAsync_TransitionsToRejectedByDepartmentHead()
    {
        _getDetailCallCount = 0;
        var pendingDetail = MakeDetailDto(ApplicationId, "PendingDepartmentHead", supervisorUserId: SupervisorUserId, supervisorDeptId: DepartmentId);
        var rejectedDetail = MakeDetailDto(ApplicationId, "RejectedByDepartmentHead", supervisorUserId: SupervisorUserId);

        _usersRepo.GetByIdAsync(DeptHeadUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithRole(DeptHeadUserId, "DepartmentHead", DepartmentId));
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _getDetailCallCount++;
                // 1=VerifyDeptHead, 2=Transition status check, 3=return DTO
                return _getDetailCallCount <= 2 ? pendingDetail : rejectedDetail;
            });
        _usersRepo.GetByIdAsync(SupervisorUserId, Arg.Any<CancellationToken>())
            .Returns(MakeUserWithDepartment(SupervisorUserId, DepartmentId));
        _appRepo.GetByIdWithTrackingAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeApplicationEntity());

        var result = await _sut.RejectByDepartmentHeadAsync(
            ApplicationId, new RejectByDepartmentHeadCommand("Doesn't meet requirements"), DeptHeadUserId, CancellationToken.None);

        result.Error.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // CancelAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CancelAsync_ReturnsNotFound_WhenApplicationNotFound()
    {
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>()).Returns((StudentApplicationDetailDto?)null);

        var result = await _sut.CancelAsync(ApplicationId, StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.NotFound);
    }

    [Fact]
    public async Task CancelAsync_ReturnsForbidden_WhenNotStudentOwner()
    {
        var otherStudentUserId = Guid.NewGuid();
        var otherStudentProfileId = Guid.NewGuid();
        _appRepo.GetStudentIdByUserIdAsync(otherStudentUserId, Arg.Any<CancellationToken>()).Returns(otherStudentProfileId);
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeDetailDto(ApplicationId, "Pending", studentId: StudentProfileId));

        var result = await _sut.CancelAsync(ApplicationId, otherStudentUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.Forbidden);
    }

    [Fact]
    public async Task CancelAsync_ReturnsInvalidTransition_WhenAlreadySentToDeptHead()
    {
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeDetailDto(ApplicationId, "PendingDepartmentHead", studentId: StudentProfileId));

        var result = await _sut.CancelAsync(ApplicationId, StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.InvalidTransition);
    }

    [Fact]
    public async Task CancelAsync_ReturnsInvalidTransition_WhenAlreadyRejected()
    {
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeDetailDto(ApplicationId, "RejectedBySupervisor", studentId: StudentProfileId));

        var result = await _sut.CancelAsync(ApplicationId, StudentUserId, CancellationToken.None);

        result.Error.Should().Be(ApplicationsError.InvalidTransition);
    }

    [Fact]
    public async Task CancelAsync_Succeeds_FromPending()
    {
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeDetailDto(ApplicationId, "Pending", studentId: StudentProfileId));
        _appRepo.GetByIdWithTrackingAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeApplicationEntity());

        var result = await _sut.CancelAsync(ApplicationId, StudentUserId, CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeTrue();
        await _appRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelAsync_Succeeds_FromApprovedBySupervisor()
    {
        _appRepo.GetDetailAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeDetailDto(ApplicationId, "ApprovedBySupervisor", studentId: StudentProfileId));
        _appRepo.GetByIdWithTrackingAsync(ApplicationId, Arg.Any<CancellationToken>())
            .Returns(MakeApplicationEntity());

        var result = await _sut.CancelAsync(ApplicationId, StudentUserId, CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Guid OtherUserId() => Guid.NewGuid();

    private static User MakeStudentUser() => new()
    {
        Id = StudentUserId,
        Email = "student@test.com",
        FirstName = "Student",
        LastName = "Test",
        RoleId = Guid.NewGuid(),
        IsActive = true,
        Role = new UserRole { Id = Guid.NewGuid(), CodeName = "Student", DisplayName = "Студент" }
    };

    private static User MakeUserWithNoDepartment(Guid userId) => new()
    {
        Id = userId,
        Email = "sup@test.com",
        FirstName = "Supervisor",
        LastName = "Test",
        RoleId = Guid.NewGuid(),
        IsActive = true,
        DepartmentId = null,
        Role = new UserRole { Id = Guid.NewGuid(), CodeName = "Teacher", DisplayName = "Преподаватель" }
    };

    private static User MakeUserWithDepartment(Guid userId, Guid deptId) => new()
    {
        Id = userId,
        Email = "sup@test.com",
        FirstName = "Supervisor",
        LastName = "Test",
        RoleId = Guid.NewGuid(),
        IsActive = true,
        DepartmentId = deptId,
        Role = new UserRole { Id = Guid.NewGuid(), CodeName = "Teacher", DisplayName = "Преподаватель" }
    };

    private static User MakeUserWithRole(Guid userId, string role, Guid? deptId) => new()
    {
        Id = userId,
        Email = "user@test.com",
        FirstName = "User",
        LastName = "Test",
        RoleId = Guid.NewGuid(),
        IsActive = true,
        DepartmentId = deptId,
        Role = new UserRole { Id = Guid.NewGuid(), CodeName = role, DisplayName = role }
    };

    private static StudentApplication MakeApplicationEntity() => new()
    {
        Id = ApplicationId,
        StudentId = StudentProfileId,
        TopicId = TopicId,
        SupervisorRequestId = SupervisorRequestId,
        StatusId = PendingStatusId,
        Student = new Student
        {
            Id = StudentProfileId,
            UserId = StudentUserId,
            User = new User
            {
                Id = StudentUserId,
                Email = "student@test.com",
                FirstName = "Student",
                LastName = "Test",
                Role = new UserRole { CodeName = "Student", DisplayName = "Студент" }
            }
        }
    };

    private static TopicDto MakeTopicDto(Guid topicId) => new(
        topicId,
        "Test Topic",
        "Description",
        new DictionaryItemRefDto(Guid.NewGuid(), "Active", "Active"),
        new DictionaryItemRefDto(Guid.NewGuid(), "Teacher", "Teacher"),
        SupervisorUserId,
        "supervisor@test.com",
        "Supervisor",
        "Test",
        DateTime.UtcNow,
        null);

    private static StudentApplicationDetailDto MakeDetailDto(
        Guid appId,
        string statusCodeName,
        Guid? studentId = null,
        Guid? supervisorUserId = null,
        Guid? supervisorDeptId = null) => new(
        appId,
        studentId ?? StudentProfileId,
        "Student", "Test", "4411",
        TopicId, "Test Topic", "Description",
        SupervisorRequestId,
        supervisorUserId ?? SupervisorUserId,
        "Supervisor",
        "Test",
        supervisorDeptId,
        supervisorUserId ?? SupervisorUserId,
        "Supervisor", "Test",
        supervisorDeptId,
        new ApplicationStatusRefDto(Guid.NewGuid(), statusCodeName, statusCodeName),
        DateTime.UtcNow, null,
        []);
}
