namespace SecureVote.Contracts.Results;

// ========== Geographic Statistics DTOs ==========

public record GovernorateStatsResponse(
    int ElectionId,
    string ElectionTitle,
    int TotalVotes,
    IEnumerable<GovernorateBreakdown> Governorates
);

public record GovernorateBreakdown(
    int GovernorateId,
    string GovernorateNameAr,
    string GovernorateNameEn,
    int TotalVotes,
    IEnumerable<CandidateBreakdown> Candidates
);

public record ConstituencyStatsResponse(
    int ElectionId,
    int GovernorateId,
    string GovernorateNameAr,
    int TotalVotes,
    IEnumerable<ConstituencyBreakdown> Constituencies
);

public record ConstituencyBreakdown(
    int ConstituencyId,
    string ConstituencyNameAr,
    string ConstituencyNameEn,
    int TotalVotes,
    IEnumerable<CandidateBreakdown> Candidates
);

public record CandidateBreakdown(
    int CandidateId,
    string CandidateName,
    string? Party,
    int VoteCount,
    decimal Percentage
);

public record ParticipationStatsResponse(
    int ElectionId,
    string ElectionTitle,
    int TotalRegistered,
    int TotalVoted,
    decimal OverallPercentage,
    IEnumerable<GovernorateParticipation> ByGovernorate
);

public record GovernorateParticipation(
    int GovernorateId,
    string GovernorateNameAr,
    string GovernorateNameEn,
    int Registered,
    int Voted,
    int MaleVoted,
    int FemaleVoted,
    decimal Percentage
);

// ========== Demographic Statistics DTOs ==========

public record DemographicStatsResponse(
    int ElectionId,
    string ElectionTitle,
    int TotalVotes,
    GenderStatsBreakdown GenderStats,
    AgeStatsBreakdown AgeStats
);

public record GenderStatsBreakdown(
    int TotalMaleVotes,
    int TotalFemaleVotes,
    decimal MalePercentage,
    decimal FemalePercentage,
    IEnumerable<CandidateGenderBreakdown> CandidateBreakdowns
);

public record CandidateGenderBreakdown(
    int CandidateId,
    string CandidateName,
    string? Party,
    int MaleVotes,
    int FemaleVotes,
    decimal MalePercentage,
    decimal FemalePercentage
);

public record AgeStatsBreakdown(
    IEnumerable<AgeGroupSummary> AgeGroups,
    IEnumerable<CandidateAgeBreakdown> CandidateBreakdowns
);

public record AgeGroupSummary(
    string AgeGroup,
    int VoteCount,
    decimal Percentage
);

public record CandidateAgeBreakdown(
    int CandidateId,
    string CandidateName,
    string? Party,
    IEnumerable<AgeGroupVotes> AgeGroups
);

public record AgeGroupVotes(
    string AgeGroup,
    int VoteCount,
    decimal Percentage
);
