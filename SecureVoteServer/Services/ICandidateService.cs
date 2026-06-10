using SecureVote.Abstractions;
using SecureVote.Contracts.Candidates;

namespace SecureVote.Services;

public interface ICandidateService
{
    Task<Result<CandidateResponse>> CreateAsync(int electionId, CreateCandidateRequest request, int organizerId);
    Task<Result<IEnumerable<CandidateResponse>>> GetByElectionIdAsync(int electionId, int organizerId);
    Task<Result<IEnumerable<CandidateResponse>>> GetPublicByElectionIdAsync(int electionId);
    Task<Result<CandidateResponse>> GetByIdAsync(int id);
    Task<Result<CandidateResponse>> UpdateAsync(int id, UpdateCandidateRequest request, int organizerId);
    Task<Result> DeleteAsync(int id, int organizerId);
    Task<Result<CandidateResponse>> UploadPhotoAsync(int id, IFormFile photo, int organizerId);
}
