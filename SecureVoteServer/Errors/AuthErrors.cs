using SecureVote.Abstractions;

namespace SecureVote.Errors;

public static class AuthErrors
{
    public static readonly Error InvalidCredentials = new("Auth.InvalidCredentials", "Invalid email or password", StatusCodes.Status401Unauthorized);
    public static readonly Error UserNotFound = new("Auth.UserNotFound", "User not found", StatusCodes.Status404NotFound);
    public static readonly Error UserDisabled = new("Auth.UserDisabled", "Account is disabled", StatusCodes.Status403Forbidden);
    public static readonly Error EmailNotConfirmed = new("Auth.EmailNotConfirmed", "Email not confirmed", StatusCodes.Status401Unauthorized);
    public static readonly Error InvalidRole = new("Auth.InvalidRole", "Invalid user role", StatusCodes.Status400BadRequest);
    public static readonly Error DuplicateEmail = new("Auth.DuplicateEmail", "Email already exists", StatusCodes.Status409Conflict);
    public static readonly Error DuplicateUsername = new("Auth.DuplicateUsername", "Username already exists", StatusCodes.Status409Conflict);
    public static readonly Error RegistrationFailed = new("Auth.RegistrationFailed", "Registration failed", StatusCodes.Status400BadRequest);

    public static Error PasswordValidationFailed(IEnumerable<Microsoft.AspNetCore.Identity.IdentityError> errors)
    {
        var errorMessages = string.Join(", ", errors.Select(e => e.Description));
        return new Error("Auth.PasswordValidationFailed", errorMessages, StatusCodes.Status400BadRequest);
    }
}

