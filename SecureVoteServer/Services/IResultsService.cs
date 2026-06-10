using SecureVote.Abstractions;
using SecureVote.Contracts.Results;

namespace SecureVote.Services;

public interface IResultsService
{
    /// <summary>
    /// Count votes for a closed election.
    /// Decrypts all ballots, tallies votes, stores results, and anonymizes ballots.
    /// </summary>
    Task<Result<CountVotesResponse>> CountVotesAsync(int electionId, int organizerId);
    
    /// <summary>
    /// Get election results after counting.
    /// </summary>
    Task<Result<ElectionResultsResponse>> GetResultsAsync(int electionId);

    Task<Result<GovernorateStatsResponse>> GetStatsByGovernorateAsync(int electionId);
    Task<Result<ConstituencyStatsResponse>> GetStatsByConstituencyAsync(int electionId, int governorateId);
    Task<Result<ParticipationStatsResponse>> GetParticipationStatsAsync(int electionId);
    Task<Result<DemographicStatsResponse>> GetDemographicStatsAsync(int electionId);
}
