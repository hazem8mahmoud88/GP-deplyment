namespace SecureVote.Contracts.Voting;

// ========== Request DTOs ==========

public record VerifyIdentityRequest(
    int ElectionId,
    string NationalId
);

public record VerifyFaceRequest(
    int ElectionId,
    string NationalId,
    string SelfieBase64  // Base64 encoded image
);

public record CastVoteRequest(
    int ElectionId,
    int CandidateId
);

// ========== Response DTOs ==========

public record VerifyIdentityResponse(
    bool IsEligible,
    string VoterName,
    string? Location,
    string Message
);

public record VerifyFaceResponse(
    bool IsVerified,
    string VoterToken,  // Short-lived JWT (5 mins)
    string Message
);

public record CastVoteResponse(
    bool Success,
    string Message,
    string? ReceiptHash  // Optional: proof of vote (hash)
);
