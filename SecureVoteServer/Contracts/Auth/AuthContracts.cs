namespace SecureVote.Contracts.Auth;

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string Token,
    string Email,
    string Username,
    string Role,
    int? AdminId,
    int? OrganizerId,
    DateTime ExpiresAt
);

public record RegisterOrganizerRequest(
    string Email,
    string Username,
    string Password,
    string FullName,
    string? Organization,
    string? PhoneNumber
);

public record RegisterAdminRequest(
    string Email,
    string Username,
    string Password,
    string FullName,
    string? Department,
    string? PhoneNumber
);
