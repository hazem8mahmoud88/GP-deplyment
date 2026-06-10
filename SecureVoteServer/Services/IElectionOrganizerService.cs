using SecureVote.Abstractions;
using SecureVote.Contracts.ElectionOrganizers;

namespace SecureVote.Services;

public interface IElectionOrganizerService
{
    Task<Result<ElectionOrganizerResponse>> AssignAsync(int electionId, AssignOrganizerRequest request, int adminId);
    Task<Result<IEnumerable<ElectionOrganizerResponse>>> GetByElectionIdAsync(int electionId);
    Task<Result> RemoveAsync(int electionId, int organizerId);
    Task<Result<IEnumerable<OrganizerSummaryResponse>>> GetAllOrganizersAsync();
}
