namespace SecureVote.Contracts.ElectionOrganizers;

// ========== Request DTOs ==========

public record AssignOrganizerRequest(
    int OrganizerId,
    bool CanDecrypt = true
);

// ========== Response DTOs ==========

public record OrganizerSummaryResponse(
    int Id,
    string FullName,
    string Email,
    string? Organization,
    string? PhoneNumber
);


public record ElectionOrganizerResponse(
    int Id,
    int ElectionId,
    int OrganizerId,
    string OrganizerName,
    string OrganizerEmail,
    int AssignedByAdminId,
    bool CanDecrypt,
    DateTime AssignedAt
);
