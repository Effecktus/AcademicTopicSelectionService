using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Auth;
using AcademicTopicSelectionService.Application.Dictionaries.UserRoles;
using AcademicTopicSelectionService.Application.Users;
using AcademicTopicSelectionService.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace AcademicTopicSelectionService.UnitTests.Users;

public sealed class UserAccountsServiceTests
{
    private readonly IUsersRepository _usersRepo = Substitute.For<IUsersRepository>();
    private readonly IUserRolesRepository _rolesRepo = Substitute.For<IUserRolesRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();

    private readonly UserAccountsService _sut;

    public UserAccountsServiceTests()
    {
        _sut = new UserAccountsService(_usersRepo, _rolesRepo, _passwordHasher);
        var roleId = AnyRoleId();
        _rolesRepo.GetAsync(roleId, Arg.Any<CancellationToken>())
            .Returns(new UserRoleDto(roleId, "Student", "Студент", DateTime.UtcNow, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("notanemail")]
    [InlineData("   ")]
    public async Task CreateAsync_ReturnsValidation_WhenEmailIsInvalid(string email)
    {
        var result = await _sut.CreateAsync(
            new CreateUserRequest(email, "password123", "Ivan", "Ivanov", null, AnyRoleId()),
            CancellationToken.None);

        result.Error.Should().Be(AuthError.Validation);
        await _usersRepo.DidNotReceive().CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("short1")]
    [InlineData("1234567890")]
    [InlineData("abcdefghij")]
    public async Task CreateAsync_ReturnsValidation_WhenPasswordInvalid(string password)
    {
        var result = await _sut.CreateAsync(
            new CreateUserRequest("test@test.com", password, "Ivan", "Ivanov", null, AnyRoleId()),
            CancellationToken.None);

        result.Error.Should().Be(AuthError.Validation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_ReturnsValidation_WhenFirstNameIsEmpty(string firstName)
    {
        var result = await _sut.CreateAsync(
            new CreateUserRequest("test@test.com", "password123", firstName, "Ivanov", null, AnyRoleId()),
            CancellationToken.None);

        result.Error.Should().Be(AuthError.Validation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_ReturnsValidation_WhenLastNameIsEmpty(string lastName)
    {
        var result = await _sut.CreateAsync(
            new CreateUserRequest("test@test.com", "password123", "Ivan", lastName, null, AnyRoleId()),
            CancellationToken.None);

        result.Error.Should().Be(AuthError.Validation);
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidation_WhenRoleIdIsEmpty()
    {
        var result = await _sut.CreateAsync(
            new CreateUserRequest("test@test.com", "password123", "Ivan", "Ivanov", null, Guid.Empty),
            CancellationToken.None);

        result.Error.Should().Be(AuthError.Validation);
    }

    [Fact]
    public async Task CreateAsync_ReturnsValidation_WhenRoleNotFound()
    {
        var unknownRoleId = Guid.NewGuid();
        _rolesRepo.GetAsync(unknownRoleId, Arg.Any<CancellationToken>()).Returns((UserRoleDto?)null);

        var result = await _sut.CreateAsync(
            new CreateUserRequest("test@test.com", "password123", "Ivan", "Ivanov", null, unknownRoleId),
            CancellationToken.None);

        result.Error.Should().Be(AuthError.Validation);
        result.Message.Should().Contain("Role");
        await _usersRepo.DidNotReceive().CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsEmailAlreadyExists_WhenEmailTaken()
    {
        _usersRepo.ExistsByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(
            new CreateUserRequest("test@test.com", "password123", "Ivan", "Ivanov", null, AnyRoleId()),
            CancellationToken.None);

        result.Error.Should().Be(AuthError.EmailAlreadyExists);
        await _usersRepo.DidNotReceive().CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ChecksDuplicateEmail_UsingNormalizedForm()
    {
        _usersRepo.ExistsByEmailAsync("test@example.com", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(
            new CreateUserRequest("  TEST@EXAMPLE.COM  ", "password123", "Ivan", "Ivanov", null, AnyRoleId()),
            CancellationToken.None);

        result.Error.Should().Be(AuthError.EmailAlreadyExists);
        await _usersRepo.Received(1).ExistsByEmailAsync("test@example.com", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedUser_WhenDataIsValid()
    {
        var roleId = AnyRoleId();
        var createdUser = MakeUser(email: "test@test.com", roleId: roleId);
        _usersRepo.ExistsByEmailAsync("test@test.com", Arg.Any<CancellationToken>()).Returns(false);
        _usersRepo.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(createdUser);

        var result = await _sut.CreateAsync(
            new CreateUserRequest("test@test.com", "password123", "Ivan", "Ivanov", null, roleId),
            CancellationToken.None);

        result.Error.Should().BeNull();
        result.Value!.UserId.Should().Be(createdUser.Id);
        result.Value.Email.Should().Be("test@test.com");
        result.Value.Role.Should().Be("Student");
    }

    [Fact]
    public async Task CreateAsync_HashesPassword_BeforeSavingUser()
    {
        _passwordHasher.Hash("password123").Returns("hashed-password");
        _usersRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _usersRepo.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(MakeUser());

        await _sut.CreateAsync(
            new CreateUserRequest("test@test.com", "password123", "Ivan", "Ivanov", null, AnyRoleId()),
            CancellationToken.None);

        await _usersRepo.Received(1).CreateAsync(
            Arg.Is<User>(u => u.PasswordHash == "hashed-password"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_NormalizesEmail_BeforeCreatingUser()
    {
        _usersRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _usersRepo.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(MakeUser());

        await _sut.CreateAsync(
            new CreateUserRequest("  TEST@EXAMPLE.COM  ", "password123", "Ivan", "Ivanov", null, AnyRoleId()),
            CancellationToken.None);

        await _usersRepo.Received(1).CreateAsync(
            Arg.Is<User>(u => u.Email == "test@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_TrimsFirstAndLastName_BeforeCreatingUser()
    {
        _usersRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _usersRepo.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(MakeUser());

        await _sut.CreateAsync(
            new CreateUserRequest("test@test.com", "password123", "  Ivan  ", "  Ivanov  ", null, AnyRoleId()),
            CancellationToken.None);

        await _usersRepo.Received(1).CreateAsync(
            Arg.Is<User>(u => u.FirstName == "Ivan" && u.LastName == "Ivanov"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_TrimsMiddleName_BeforeCreatingUser()
    {
        _usersRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _usersRepo.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(MakeUser());

        await _sut.CreateAsync(
            new CreateUserRequest("test@test.com", "password123", "Ivan", "Ivanov", "  Petrovich  ", AnyRoleId()),
            CancellationToken.None);

        await _usersRepo.Received(1).CreateAsync(
            Arg.Is<User>(u => u.MiddleName == "Petrovich"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_SetsMiddleNameNull_WhenWhitespaceProvided()
    {
        _usersRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _usersRepo.CreateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(MakeUser());

        await _sut.CreateAsync(
            new CreateUserRequest("test@test.com", "password123", "Ivan", "Ivanov", "   ", AnyRoleId()),
            CancellationToken.None);

        await _usersRepo.Received(1).CreateAsync(
            Arg.Is<User>(u => u.MiddleName == null),
            Arg.Any<CancellationToken>());
    }

    private static Guid AnyRoleId() => Guid.Parse("a1111111-1111-1111-1111-111111111111");

    private static User MakeUser(
        string email = "user@test.com",
        bool isActive = true,
        Guid? id = null,
        Guid? roleId = null)
    {
        var rid = roleId ?? AnyRoleId();
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            Email = email,
            PasswordHash = "hashed-password",
            FirstName = "Ivan",
            LastName = "Ivanov",
            RoleId = rid,
            IsActive = isActive,
            Role = new UserRole { Id = rid, CodeName = "Student", DisplayName = "Студент" }
        };
    }
}
