namespace SecureVote.Contracts.Elections;

// ========== Request DTOs ==========

public record CreateElectionRequest(
    string Title,
    string Type,
    string? Description,
    DateTime StartDate,
    DateTime EndDate
);

public record UpdateElectionRequest(
    string Title,
    string Type,
    string? Description,
    DateTime StartDate,
    DateTime EndDate
);

// ========== Response DTOs ==========

public record ElectionResponse(
    int Id,
    string Title,
    string Type,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    string PublicKey,
    int CreatedByAdminId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int CandidatesCount,
    int OrganizersCount,
    int VotersCount
);

public record ElectionSummaryResponse(
    int Id,
    string Title,
    string Type,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    int CandidatesCount,
    int VotersCount
);
