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
        entity.FilePath.Should().Be($"graduate-works/{id:D}/{GraduateWorksFileTypes.Thesis}");
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
        entity.PresentationPath.Should().Be($"graduate-works/{id:D}/{GraduateWorksFileTypes.Presentation}");
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
                FilePath = $"graduate-works/{id:D}/{GraduateWorksFileTypes.Thesis}",
                FileName = "Диплом.docx"
            });
        _files.GenerateDownloadUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new FileUrlDto("https://example/download", DateTime.UtcNow.AddMinutes(15)));

        var sut = CreateSut();
        var result = await sut.GetDownloadUrlAsync(id, GraduateWorksFileTypes.Thesis, CancellationToken.None);

        result.Error.Should().BeNull();
        await _files.Received(1).GenerateDownloadUrlAsync(
            $"graduate-works/{id:D}/{GraduateWorksFileTypes.Thesis}",
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
            FilePath = "graduate-works/a/thesis",
            PresentationPath = "graduate-works/a/presentation"
        };
        _repo.GetByIdTrackedAsync(id, Arg.Any<CancellationToken>()).Returns(entity);

        var sut = CreateSut();
        var result = await sut.DeleteAsync(id, CancellationToken.None);

        result.Error.Should().BeNull();
        await _files.Received(1).DeleteObjectAsync("graduate-works/a/thesis", Arg.Any<CancellationToken>());
        await _files.Received(1).DeleteObjectAsync("graduate-works/a/presentation", Arg.Any<CancellationToken>());
        await _repo.Received(1).DeleteAsync(entity, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUploadUrlAsync_ReturnsUrl_WhenWorkExists()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(new GraduateWorkDto(
                id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "T", 2025, 50, "C", false, false,
                DateTime.UtcNow, null));
        _files.GenerateUploadUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new FileUrlDto("https://example/upload", DateTime.UtcNow.AddMinutes(15)));

        var sut = CreateSut();
        var result = await sut.GetUploadUrlAsync(id, "THESIS", CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value!.Url.Should().NotBeNullOrEmpty();
        await _files.Received(1).GenerateUploadUrlAsync(
            $"graduate-works/{id:D}/{GraduateWorksFileTypes.Thesis}",
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }
}
