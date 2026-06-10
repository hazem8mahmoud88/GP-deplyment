using SecureVote.Abstractions;
using SecureVote.Contracts.Voting;

namespace SecureVote.Services;

public interface IVotingService
{
    Task<Result<VerifyIdentityResponse>> VerifyIdentityAsync(VerifyIdentityRequest request);
    Task<Result<VerifyFaceResponse>> VerifyFaceAsync(VerifyFaceRequest request);
    Task<Result<CastVoteResponse>> CastVoteAsync(CastVoteRequest request, string votingToken);
}
