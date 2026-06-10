using SecureVote.Abstractions;
using SecureVote.Contracts.Organizer;

namespace SecureVote.Services;

public interface IVoterUploadService
{
    Task<Result<UploadVotersResponse>> UploadVotersCsvAsync(int electionId, IFormFile csvFile, int organizerId);
    Task<Result<UploadPhotosResponse>> UploadPhotosZipAsync(int electionId, IFormFile zipFile, int organizerId);
    Task<Result<ElectionStatsResponse>> GetElectionStatsAsync(int electionId, int organizerId);
}
