using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.GraduateWorks;
using AcademicTopicSelectionService.Application.Notifications;
using AcademicTopicSelectionService.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.GraduateWorks;

public sealed class GraduateWorksServiceTests
{
    private readonly IGraduateWorksRepository _repo = Substitute.For<IGraduateWorksRepository>();
    private readonly IFileStorageService _files = Substitute.For<IFileStorageService>();
    private readonly INotificationsService _notifications = Substitute.For<INotificationsService>();

    private GraduateWorksService CreateSut() => new(_repo, _files, _notifications);

    [Fact]
    public async Task CreateAsync_ReturnsValidation_WhenApplicationIdEmpty()
    {
        var sut = CreateSut();
        var result = await sut.CreateAsync(
            new CreateGraduateWorkCommand(Guid.Empty, "Title", 2025, 75, "Commission"), CancellationToken.None);

        result.Error.Should().Be(GraduateWorksError.Validation);
        result.Message.Should().Contain("ApplicationId is required");
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidation_WhenDuplicateApplication()
    {
        var appId = Guid.NewGuid();
        _repo.ExistsForApplicationAsync(appId, Arg.Any<CancellationToken>()).Returns(true);

        var sut = CreateSut();
        var result = await sut.CreateAsync(
            new CreateGraduateWorkCommand(appId, "Title", 2025, 75, "Commission"), CancellationToken.None);

        result.Error.Should().Be(GraduateWorksError.Validation);
        result.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidation_WhenArchiveContextMissing()
    {
        var appId = Guid.NewGuid();
        _repo.ExistsForApplicationAsync(appId, Arg.Any<CancellationToken>()).Returns(false);
        _repo.GetArchiveContextByApplicationIdAsync(appId, Arg.Any<CancellationToken>())
            .Returns((GraduateWorkArchiveContext?)null);

        var sut = CreateSut();
        var result = await sut.CreateAsync(
            new CreateGraduateWorkCommand(appId, "Title", 2025, 75, "Commission"), CancellationToken.None);

        result.Error.Should().Be(GraduateWorksError.Validation);
        result.Message.Should().Contain("Application not found");
    }

    [Fact]
    public async Task GetUploadUrlAsync_ReturnsNotFound_WhenWorkMissing()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((GraduateWorkDto?)null);

        var sut = CreateSut();
        var result = await sut.GetUploadUrlAsync(id, GraduateWorksFileTypes.Thesis, CancellationToken.None);

        result.Error.Should().Be(GraduateWorksError.NotFound);
    }

    [Fact]
    public async Task GetUploadUrlAsync_ReturnsValidation_WhenFileTypeInvalid()
    {
        var sut = CreateSut();
        var result = await sut.GetUploadUrlAsync(Guid.NewGuid(), "invalid", CancellationToken.None);

        result.Error.Should().Be(GraduateWorksError.Validation);
        result.Message.Should().Contain("fileType must be 'thesis' or 'presentation'");
    }

    [Fact]
    public async Task ConfirmUploadAsync_ReturnsValidation_WhenObjectMissingInStorage()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdTrackedAsync(id, Arg.Any<CancellationToken>())
            .Returns(new GraduateWork { Id = id, ApplicationId = Guid.NewGuid() });
        _files.ObjectExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var sut = CreateSut();
        var result = await sut.ConfirmUploadAsync(id, GraduateWorksFileTypes.Thesis, "thesis.docx", CancellationToken.None);

        result.Error.Should().Be(GraduateWorksError.Validation);
        result.Message.Should().Contain("Object not found");
        await _notifications.DidNotReceive()
            .CreateAsync(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmUploadAsync_ReturnsValidation_WhenFileNameEmpty()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdTrackedAsync(id, Arg.Any<CancellationToken>())
            .Returns(new GraduateWork { Id = id, ApplicationId = Guid.NewGuid() });

        var sut = CreateSut();
        var result = await sut.ConfirmUploadAsync(id, GraduateWorksFileTypes.Thesis, "   ", CancellationToken.None);

        result.Error.Should().Be(GraduateWorksError.Validation);
        result.Message.Should().Contain("FileName is required");
    }

    [Fact]
    public async Task ConfirmUploadAsync_ReturnsValidation_WhenFileNameTooLong()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdTrackedAsync(id, Arg.Any<CancellationToken>())
            .Returns(new GraduateWork { Id = id, ApplicationId = Guid.NewGuid() });

        var sut = CreateSut();
        var result = await sut.ConfirmUploadAsync(
            id, GraduateWorksFileTypes.Thesis, new string('a', 256), CancellationToken.None);

        result.Error.Should().Be(GraduateWorksError.Validation);
        result.Message.Should().Contain("<= 255");
    }

    [Fact]
    public async Task ConfirmUploadAsync_ReturnsNotFound_WhenWorkMissing()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdTrackedAsync(id, Arg.Any<CancellationToken>()).Returns((GraduateWork?)null);

        var sut = CreateSut();
        var result = await sut.ConfirmUploadAsync(id, GraduateWorksFileTypes.Thesis, "thesis.docx", CancellationToken.None);

        result.Error.Should().Be(GraduateWorksError.NotFound);
    }

    [Fact]
    public async Task ConfirmUploadAsync_StoresThesisPathAndFileName_WhenObjectExists()
    {
        var id = Guid.NewGuid();
        var studentProfileId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();
        var entity = new GraduateWork
        {
            Id = id,
            ApplicationId = Guid.NewGuid(),
            StudentId = studentProfileId,
            Title = "Работа тест"
        };
        _repo.GetByIdTrackedAsync(id, Arg.Any<CancellationToken>()).Returns(entity);
        _files.ObjectExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _repo.GetStudentUserIdByStudentProfileIdAsync(studentProfileId, Arg.Any<CancellationToken>())
            .Returns(studentUserId);
        _notifications.CreateAsync(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var cmd = ci.ArgAt<CreateNotificationCommand>(0);
                return Task.FromResult<Notification?>(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = cmd.UserId,
                    Title = cmd.Title,
                    Content = cmd.Content
                });
            });

        var sut = CreateSut();
        var result = await sut.ConfirmUploadAsync(id, GraduateWorksFileTypes.Thesis, " thesis.docx ", CancellationToken.None);

        result.Error.Should().BeNull();
        entity.FilePath.Should().Be($"{id:D}/{GraduateWorksFileTypes.Thesis}");
        entity.FileName.Should().Be("thesis.docx");
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notifications.Received(1).CreateAsync(
            Arg.Is<CreateNotificationCommand>(c =>
                c.UserId == studentUserId &&
                c.TypeCodeName == NotificationTypeCodes.GraduateWorkUploaded &&
                c.Content.Contains("thesis.docx") &&
                c.Content.Contains("Работа тест")),
            Arg.Any<CancellationToken>());
        await _notifications.Received(1).EnqueueEmailAsync(
            studentUserId, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmUploadAsync_StoresPresentationPathAndFileName_WhenObjectExists()
    {
        var id = Guid.NewGuid();
        var studentProfileId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();
        var entity = new GraduateWork
        {
            Id = id,
            ApplicationId = Guid.NewGuid(),
            StudentId = studentProfileId,
            Title = "Презентация тест"
        };
        _repo.GetByIdTrackedAsync(id, Arg.Any<CancellationToken>()).Returns(entity);
        _files.ObjectExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _repo.GetStudentUserIdByStudentProfileIdAsync(studentProfileId, Arg.Any<CancellationToken>())
            .Returns(studentUserId);
        _notifications.CreateAsync(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var cmd = ci.ArgAt<CreateNotificationCommand>(0);
                return Task.FromResult<Notification?>(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = cmd.UserId,
                    Title = cmd.Title,
                    Content = cmd.Content
                });
            });

        var sut = CreateSut();
        var result = await sut.ConfirmUploadAsync(id, GraduateWorksFileTypes.Presentation, "deck.pptx", CancellationToken.None);

        result.Error.Should().BeNull();
        entity.PresentationPath.Should().Be($"{id:D}/{GraduateWorksFileTypes.Presentation}");
        entity.PresentationFileName.Should().Be("deck.pptx");
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notifications.Received(1).CreateAsync(
            Arg.Is<CreateNotificationCommand>(c =>
                c.TypeCodeName == NotificationTypeCodes.GraduateWorkUploaded &&
                c.Content.Contains("презентация")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDownloadUrlAsync_ReturnsValidation_WhenThesisNotUploaded()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdTrackedAsync(id, Arg.Any<CancellationToken>())
            .Returns(new GraduateWork { Id = id, ApplicationId = Guid.NewGuid(), FilePath = null });

        var sut = CreateSut();
        var result = await sut.GetDownloadUrlAsync(id, GraduateWorksFileTypes.Thesis, CancellationToken.None);

        result.Error.Should().Be(GraduateWorksError.Validation);
        result.Message.Should().Contain("not uploaded");
    }

    [Fact]
    public async Task GetDownloadUrlAsync_PassesStoredFileNameToStorage()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdTrackedAsync(id, Arg.Any<CancellationToken>())
            .Returns(new GraduateWork
            {
                Id = id,
                ApplicationId = Guid.NewGuid(),
                FilePath = $"{id:D}/{GraduateWorksFileTypes.Thesis}",
                FileName = "Диплом.docx"
            });
        _files.GenerateDownloadUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new FileUrlDto("https://example/download", DateTime.UtcNow.AddMinutes(15)));

        var sut = CreateSut();
        var result = await sut.GetDownloadUrlAsync(id, GraduateWorksFileTypes.Thesis, CancellationToken.None);

        result.Error.Should().BeNull();
        await _files.Received(1).GenerateDownloadUrlAsync(
            $"{id:D}/{GraduateWorksFileTypes.Thesis}",
            Arg.Any<TimeSpan>(),
            "Диплом.docx",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_DeletesBothObjects_WhenPathsPresent()
    {
        var id = Guid.NewGuid();
        var entity = new GraduateWork
        {
            Id = id,
            ApplicationId = Guid.NewGuid(),
            FilePath = "a/thesis",
            PresentationPath = "a/presentation"
        };
        _repo.GetByIdTrackedAsync(id, Arg.Any<CancellationToken>()).Returns(entity);

        var sut = CreateSut();
        var result = await sut.DeleteAsync(id, CancellationToken.None);

        result.Error.Should().BeNull();
        await _files.Received(1).DeleteObjectAsync("a/thesis", Arg.Any<CancellationToken>());
        await _files.Received(1).DeleteObjectAsync("a/presentation", Arg.Any<CancellationToken>());
        await _repo.Received(1).DeleteAsync(entity, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUploadUrlAsync_ReturnsUrl_WhenWorkExists()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(new GraduateWorkDto(
                id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "T", 2025, 50, "C", "Draft", "Черновик", false, false,
                DateTime.UtcNow, null, null, null, "Студент Т.", "Преподаватель Т."));
        _files.GenerateUploadUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new FileUrlDto("https://example/upload", DateTime.UtcNow.AddMinutes(15)));

        var sut = CreateSut();
        var result = await sut.GetUploadUrlAsync(id, "THESIS", CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value!.Url.Should().NotBeNullOrEmpty();
        await _files.Received(1).GenerateUploadUrlAsync(
            $"{id:D}/{GraduateWorksFileTypes.Thesis}",
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUploadUrlAsync_UsesKeyWithoutBucketPrefix_ForThesis()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(new GraduateWorkDto(
                id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "T", 2025, 50, "C", "Draft", "Черновик", false, false,
                DateTime.UtcNow, null, null, null, "Студент Т.", "Преподаватель Т."));
        _files.GenerateUploadUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new FileUrlDto("https://example/upload", DateTime.UtcNow.AddMinutes(15)));

        var sut = CreateSut();
        var result = await sut.GetUploadUrlAsync(id, GraduateWorksFileTypes.Thesis, CancellationToken.None);

        result.Error.Should().BeNull();
        await _files.Received(1).GenerateUploadUrlAsync(
            $"{id:D}/{GraduateWorksFileTypes.Thesis}",
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
        await _files.DidNotReceive().GenerateUploadUrlAsync(
            Arg.Is<string>(k => k.StartsWith("graduate-works/")),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUploadUrlAsync_UsesKeyWithoutBucketPrefix_ForPresentation()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(new GraduateWorkDto(
                id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "T", 2025, 50, "C", "Draft", "Черновик", false, false,
                DateTime.UtcNow, null, null, null, "Студент Т.", "Преподаватель Т."));
        _files.GenerateUploadUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new FileUrlDto("https://example/upload", DateTime.UtcNow.AddMinutes(15)));

        var sut = CreateSut();
        var result = await sut.GetUploadUrlAsync(id, GraduateWorksFileTypes.Presentation, CancellationToken.None);

        result.Error.Should().BeNull();
        await _files.Received(1).GenerateUploadUrlAsync(
            $"{id:D}/{GraduateWorksFileTypes.Presentation}",
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
        await _files.DidNotReceive().GenerateUploadUrlAsync(
            Arg.Is<string>(k => k.StartsWith("graduate-works/")),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // CreateAsync — статус Draft
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ReturnsValidation_WhenDraftStatusIdNotFound()
    {
        var appId = Guid.NewGuid();
        _repo.ExistsForApplicationAsync(appId, Arg.Any<CancellationToken>()).Returns(false);
        _repo.GetArchiveContextByApplicationIdAsync(appId, Arg.Any<CancellationToken>())
            .Returns(new GraduateWorkArchiveContext(Guid.NewGuid(), Guid.NewGuid(), "Тема"));
        _repo.GetStatusIdByCodeNameAsync(GraduateWorkStatusCodes.Draft, Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var sut = CreateSut();
        var result = await sut.CreateAsync(
            new CreateGraduateWorkCommand(appId, "Тема работы", 2025, null, null), CancellationToken.None);

        result.Error.Should().Be(GraduateWorksError.Validation);
        result.Message.Should().Contain("Draft");
        await _repo.DidNotReceive().AddAsync(Arg.Any<GraduateWork>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_SetsDraftStatusId_WhenCreating()
    {
        var appId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var draftStatusId = Guid.NewGuid();

        _repo.ExistsForApplicationAsync(appId, Arg.Any<CancellationToken>()).Returns(false);
        _repo.GetArchiveContextByApplicationIdAsync(appId, Arg.Any<CancellationToken>())
            .Returns(new GraduateWorkArchiveContext(studentId, teacherId, "Тема"));
        _repo.GetStatusIdByCodeNameAsync(GraduateWorkStatusCodes.Draft, Arg.Any<CancellationToken>())
            .Returns(draftStatusId);
        _repo.AddAsync(Arg.Any<GraduateWork>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<GraduateWork>());
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new GraduateWorkDto(
                Guid.NewGuid(), appId, studentId, teacherId, "Тема работы", 2025, null, null,
                "Draft", "Черновик", false, false, DateTime.UtcNow, null, null, null,
                "Студент", "Преподаватель"));

        var sut = CreateSut();
        var result = await sut.CreateAsync(
            new CreateGraduateWorkCommand(appId, "Тема работы", 2025, null, null), CancellationToken.None);

        result.Error.Should().BeNull();
        await _repo.Received(1).AddAsync(
            Arg.Is<GraduateWork>(e => e.StatusId == draftStatusId && e.StudentId == studentId && e.TeacherId == teacherId),
            Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // UpdateAsync — автопереход в Completed
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_TransitionsToCompleted_WhenAllFieldsPresent()
    {
        var id = Guid.NewGuid();
        var completedStatusId = Guid.NewGuid();
        var entity = new GraduateWork
        {
            Id = id,
            ApplicationId = Guid.NewGuid(),
            FilePath = $"{id:D}/{GraduateWorksFileTypes.Thesis}"
        };
        _repo.GetByIdTrackedAsync(id, Arg.Any<CancellationToken>()).Returns(entity);
        _repo.GetStatusIdByCodeNameAsync(GraduateWorkStatusCodes.Completed, Arg.Any<CancellationToken>())
            .Returns(completedStatusId);
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(new GraduateWorkDto(
                id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Тема", 2025, 90, "Иванов",
                "Completed", "Заполнено", true, false, DateTime.UtcNow, null, null, null,
                "Студент", "Преподаватель"));

        var sut = CreateSut();
        var result = await sut.UpdateAsync(
            new UpdateGraduateWorkCommand(id, "Тема", 2025, 90, "Иванов"), CancellationToken.None);

        result.Error.Should().BeNull();
        entity.StatusId.Should().Be(completedStatusId);
        await _repo.Received(1)
            .GetStatusIdByCodeNameAsync(GraduateWorkStatusCodes.Completed, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_DoesNotTransition_WhenGradeMissing()
    {
        var id = Guid.NewGuid();
        var entity = new GraduateWork
        {
            Id = id,
            ApplicationId = Guid.NewGuid(),
            FilePath = $"{id:D}/{GraduateWorksFileTypes.Thesis}"
        };
        _repo.GetByIdTrackedAsync(id, Arg.Any<CancellationToken>()).Returns(entity);
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(new GraduateWorkDto(
                id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Тема", 2025, null, "Иванов",
                "Draft", "Черновик", true, false, DateTime.UtcNow, null, null, null,
                "Студент", "Преподаватель"));

        var sut = CreateSut();
        var result = await sut.UpdateAsync(
            new UpdateGraduateWorkCommand(id, "Тема", 2025, null, "Иванов"), CancellationToken.None);

        result.Error.Should().BeNull();
        await _repo.DidNotReceive()
            .GetStatusIdByCodeNameAsync(GraduateWorkStatusCodes.Completed, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_DoesNotTransition_WhenCommissionMembersMissing()
    {
        var id = Guid.NewGuid();
        var entity = new GraduateWork
        {
            Id = id,
            ApplicationId = Guid.NewGuid(),
            FilePath = $"{id:D}/{GraduateWorksFileTypes.Thesis}"
        };
        _repo.GetByIdTrackedAsync(id, Arg.Any<CancellationToken>()).Returns(entity);
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(new GraduateWorkDto(
                id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Тема", 2025, 85, null,
                "Draft", "Черновик", true, false, DateTime.UtcNow, null, null, null,
                "Студент", "Преподаватель"));

        var sut = CreateSut();
        var result = await sut.UpdateAsync(
            new UpdateGraduateWorkCommand(id, "Тема", 2025, 85, null), CancellationToken.None);

        result.Error.Should().BeNull();
        await _repo.DidNotReceive()
            .GetStatusIdByCodeNameAsync(GraduateWorkStatusCodes.Completed, Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // ConfirmUploadAsync — автопереход в Completed
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ConfirmUploadAsync_TransitionsToCompleted_WhenGradeAndCommissionAlreadySet()
    {
        var id = Guid.NewGuid();
        var completedStatusId = Guid.NewGuid();
        var studentProfileId = Guid.NewGuid();
        var entity = new GraduateWork
        {
            Id = id,
            ApplicationId = Guid.NewGuid(),
            StudentId = studentProfileId,
            Title = "Тест",
            Grade = 80,
            CommissionMembers = "Иванов И.И.",
            FilePath = null
        };
        _repo.GetByIdTrackedAsync(id, Arg.Any<CancellationToken>()).Returns(entity);
        _files.ObjectExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _repo.GetStatusIdByCodeNameAsync(GraduateWorkStatusCodes.Completed, Arg.Any<CancellationToken>())
            .Returns(completedStatusId);
        _repo.GetStudentUserIdByStudentProfileIdAsync(studentProfileId, Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var sut = CreateSut();
        var result = await sut.ConfirmUploadAsync(id, GraduateWorksFileTypes.Thesis, "thesis.pdf", CancellationToken.None);

        result.Error.Should().BeNull();
        entity.StatusId.Should().Be(completedStatusId);
        await _repo.Received(1)
            .GetStatusIdByCodeNameAsync(GraduateWorkStatusCodes.Completed, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmUploadAsync_DoesNotTransition_WhenGradeMissing()
    {
        var id = Guid.NewGuid();
        var studentProfileId = Guid.NewGuid();
        var entity = new GraduateWork
        {
            Id = id,
            ApplicationId = Guid.NewGuid(),
            StudentId = studentProfileId,
            Title = "Тест",
            Grade = null,
            CommissionMembers = "Иванов И.И.",
            FilePath = null
        };
        _repo.GetByIdTrackedAsync(id, Arg.Any<CancellationToken>()).Returns(entity);
        _files.ObjectExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _repo.GetStudentUserIdByStudentProfileIdAsync(studentProfileId, Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var sut = CreateSut();
        var result = await sut.ConfirmUploadAsync(id, GraduateWorksFileTypes.Thesis, "thesis.pdf", CancellationToken.None);

        result.Error.Should().BeNull();
        await _repo.DidNotReceive()
            .GetStatusIdByCodeNameAsync(GraduateWorkStatusCodes.Completed, Arg.Any<CancellationToken>());
    }
}
