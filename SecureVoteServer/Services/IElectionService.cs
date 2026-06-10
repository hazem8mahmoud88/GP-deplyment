using SecureVote.Abstractions;
using SecureVote.Contracts.Elections;

namespace SecureVote.Services;

public interface IElectionService
{
    Task<Result<ElectionResponse>> CreateAsync(CreateElectionRequest request, int adminId);
    Task<Result<IEnumerable<ElectionSummaryResponse>>> GetAllAsync();
    Task<Result<ElectionResponse>> GetByIdAsync(int id);
    Task<Result<ElectionResponse>> UpdateAsync(int id, UpdateElectionRequest request);
    Task<Result> ActivateAsync(int id);
    Task<Result> CloseAsync(int id);
    Task<Result> DeleteAsync(int id);
    Task<Result<IEnumerable<ElectionSummaryResponse>>> GetByOrganizerIdAsync(int organizerId);
}
