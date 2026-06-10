namespace SecureVote.Contracts.Results;

// ========== Request DTOs ==========

// No request body needed - electionId comes from route

// ========== Response DTOs ==========

public record CountVotesResponse(
    bool Success,
    string Message,
    int TotalVotesCounted,
    int FailedBallots,
    int TotalCandidates,
    DateTime CountedAt
);

public record ElectionResultsResponse(
    int ElectionId,
    string ElectionTitle,
    string ElectionType,
    string Status,
    int TotalVotes,
    DateTime? CountedAt,
    IEnumerable<CandidateResultResponse> CandidateResults
);

public record CandidateResultResponse(
    int CandidateId,
    string CandidateName,
    string? Party,
    string? PhotoUrl,
    int VoteCount,
    decimal Percentage
);
