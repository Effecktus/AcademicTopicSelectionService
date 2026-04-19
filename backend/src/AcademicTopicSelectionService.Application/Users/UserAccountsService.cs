using AcademicTopicSelectionService.Application.Abstractions;
using AcademicTopicSelectionService.Application.Auth;
using AcademicTopicSelectionService.Application.Dictionaries;
using AcademicTopicSelectionService.Application.Security;
using AcademicTopicSelectionService.Domain.Entities;

namespace AcademicTopicSelectionService.Application.Users;

/// <inheritdoc />
public sealed class UserAccountsService(
    IUsersRepository usersRepo,
    IUserRolesRepository rolesRepo,
    IPasswordHasher passwordHasher) : IUserAccountsService
{
    /// <inheritdoc />
    public async Task<Result<CreatedUserDto, AuthError>> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        if (!CredentialValidation.TryNormalizeEmail(request.Email, out var email, out var emailError))
            return Fail(AuthError.Validation, emailError ?? "Email is invalid");

        var validationError = Validate(request);
        if (validationError is not null)
            return Fail(AuthError.Validation, validationError);

        var role = await rolesRepo.GetAsync(request.RoleId, ct);
        if (role is null)
            return Fail(AuthError.Validation, "Role not found");

        if (await usersRepo.ExistsByEmailAsync(email, ct))
            return Fail(AuthError.EmailAlreadyExists, "User with this email already exists");

        var user = new User
        {
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim(),
            RoleId = request.RoleId,
            IsActive = true
        };

        var created = await usersRepo.CreateAsync(user, ct);
        return Result<CreatedUserDto, AuthError>.Ok(
            new CreatedUserDto(created.Id, created.Email, created.Role.CodeName));
    }

    private static string? Validate(CreateUserRequest req)
    {
        var passwordError = CredentialValidation.ValidatePasswordForNewUser(req.Password);
        if (passwordError is not null)
            return passwordError;
        if (string.IsNullOrWhiteSpace(req.FirstName))
            return "FirstName is required";
        if (string.IsNullOrWhiteSpace(req.LastName))
            return "LastName is required";
        if (req.RoleId == Guid.Empty)
            return "RoleId is required";
        return null;
    }

    private static Result<CreatedUserDto, AuthError> Fail(AuthError error, string message) =>
        Result<CreatedUserDto, AuthError>.Fail(error, message);
}
