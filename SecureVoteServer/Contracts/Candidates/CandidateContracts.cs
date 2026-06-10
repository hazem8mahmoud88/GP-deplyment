namespace SecureVote.Contracts.Candidates;

// ========== Request DTOs ==========

public record CreateCandidateRequest(
    string FullName,
    string? Symbol,
    string? PartyName,
    int OrderNumber,
    int? ConstituencyId // null for presidential, required for parliamentary
);

public record UpdateCandidateRequest(
    string FullName,
    string? Symbol,
    string? PartyName,
    int OrderNumber,
    int? ConstituencyId
);

// ========== Response DTOs ==========

public record CandidateResponse(
    int Id,
    int ElectionId,
    string FullName,
    string? Symbol,
    string? PartyName,
    string? PhotoPath,
    int OrderNumber,
    int? ConstituencyId,
    string? ConstituencyName
);
