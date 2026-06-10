using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SecureVote.Abstractions;
using SecureVote.Authentication;
using SecureVote.Contracts.Voting;
using SecureVote.Entities;
using SecureVote.Errors;
using SecureVote.Persistence;

namespace SecureVote.Services;

public class VotingService(
    ApplicationDbContext context,
    IFaceRecognitionService faceRecognitionService,
    IJwtProvider jwtProvider,
    IWebHostEnvironment environment) : IVotingService
{
    public async Task<Result<VerifyIdentityResponse>> VerifyIdentityAsync(VerifyIdentityRequest request)
    {
        // 1. Check if election exists and is active
        var election = await context.Elections
            .FirstOrDefaultAsync(e => e.Id == request.ElectionId);

        if (election is null)
            return Result.Failure<VerifyIdentityResponse>(VotingErrors.ElectionNotFound);

        if (election.Status != ElectionStatus.Active)
            return Result.Failure<VerifyIdentityResponse>(VotingErrors.ElectionNotActive);

        // 2. Find voter by National ID in this election
        var electionVoter = await context.ElectionVoters
            .Include(ev => ev.Voter)
                .ThenInclude(v => v.Governorate)
            .Include(ev => ev.Voter)
                .ThenInclude(v => v.Constituency)
            .FirstOrDefaultAsync(ev => 
                ev.ElectionId == request.ElectionId && 
                ev.Voter.UniqueIdentifier == request.NationalId);

        if (electionVoter is null)
            return Result.Failure<VerifyIdentityResponse>(VotingErrors.VoterNotFound);

        // 3. Check if already voted
        if (electionVoter.HasVoted)
            return Result.Failure<VerifyIdentityResponse>(VotingErrors.AlreadyVoted);

        // 4. Check eligibility
        if (!electionVoter.IsEligible)
            return Result.Failure<VerifyIdentityResponse>(VotingErrors.VoterNotFound);

        // 5. Build location string from voter's geographic data
        var location = BuildLocationString(electionVoter.Voter);

        return Result.Success(new VerifyIdentityResponse(
            IsEligible: true,
            VoterName: electionVoter.Voter.FullName ?? "Voter",
            Location: location,
            Message: "Identity verified. Please proceed to face verification."
        ));
    }

    public async Task<Result<VerifyFaceResponse>> VerifyFaceAsync(VerifyFaceRequest request)
    {
        // 1. Find voter again (double-check)
        var electionVoter = await context.ElectionVoters
            .Include(ev => ev.Voter)
            .FirstOrDefaultAsync(ev => 
                ev.ElectionId == request.ElectionId && 
                ev.Voter.UniqueIdentifier == request.NationalId);

        if (electionVoter is null)
            return Result.Failure<VerifyFaceResponse>(VotingErrors.VoterNotFound);

        if (electionVoter.HasVoted)
            return Result.Failure<VerifyFaceResponse>(VotingErrors.AlreadyVoted);

        // 2. Check if photo exists
        var photoPath = electionVoter.Voter.PhotoUrl;
        if (string.IsNullOrEmpty(photoPath))
            return Result.Failure<VerifyFaceResponse>(VotingErrors.VoterPhotoNotFound);

        // Build full path (photos are stored in /uploads/voters/...)
        var fullPhotoPath = Path.Combine(environment.ContentRootPath, photoPath.TrimStart('/'));
        if (!File.Exists(fullPhotoPath))
            return Result.Failure<VerifyFaceResponse>(VotingErrors.VoterPhotoNotFound);

        // 3. Decode selfie from Base64
        byte[] selfieBytes;
        try
        {
            // Strip the data URL prefix if present (e.g., "data:image/jpeg;base64,")
            var base64Data = request.SelfieBase64;
            if (base64Data.Contains(","))
                base64Data = base64Data.Split(',')[1];

            selfieBytes = Convert.FromBase64String(base64Data);
        }
        catch
        {
            return Result.Failure<VerifyFaceResponse>(VotingErrors.FaceVerificationFailed);
        }

        // 4. Call Face Recognition Service
        var isMatch = await faceRecognitionService.VerifyFaceAsync(fullPhotoPath, selfieBytes);
        if (!isMatch)
            return Result.Failure<VerifyFaceResponse>(VotingErrors.FaceVerificationFailed);

        // 5. Generate short-lived voting token (5 minutes)
        var votingToken = jwtProvider.GenerateVotingToken(electionVoter.Id, request.ElectionId);

        return Result.Success(new VerifyFaceResponse(
            IsVerified: true,
            VoterToken: votingToken,
            Message: "Face verified successfully. You have 5 minutes to cast your vote."
        ));
    }

    public async Task<Result<CastVoteResponse>> CastVoteAsync(CastVoteRequest request, string votingToken)
    {
        // 1. Validate voting token
        var principal = jwtProvider.ValidateToken(votingToken);
        if (principal is null)
            return Result.Failure<CastVoteResponse>(VotingErrors.InvalidToken);

        var tokenType = principal.FindFirst("TokenType")?.Value;
        if (tokenType != "Voting")
            return Result.Failure<CastVoteResponse>(VotingErrors.InvalidToken);

        var electionVoterIdClaim = principal.FindFirst("ElectionVoterId")?.Value;
        var electionIdClaim = principal.FindFirst("ElectionId")?.Value;

        if (!int.TryParse(electionVoterIdClaim, out var electionVoterId) ||
            !int.TryParse(electionIdClaim, out var tokenElectionId))
            return Result.Failure<CastVoteResponse>(VotingErrors.InvalidToken);

        // 2. Verify election ID matches
        if (tokenElectionId != request.ElectionId)
            return Result.Failure<CastVoteResponse>(VotingErrors.InvalidToken);

        // 3. Get election and verify it's still active
        var election = await context.Elections
            .FirstOrDefaultAsync(e => e.Id == request.ElectionId);

        if (election is null)
            return Result.Failure<CastVoteResponse>(VotingErrors.ElectionNotFound);

        if (election.Status != ElectionStatus.Active)
            return Result.Failure<CastVoteResponse>(VotingErrors.ElectionNotActive);

        // 4. Get election voter and verify not already voted
        var electionVoter = await context.ElectionVoters
            .FirstOrDefaultAsync(ev => ev.Id == electionVoterId);

        if (electionVoter is null)
            return Result.Failure<CastVoteResponse>(VotingErrors.VoterNotFound);

        if (electionVoter.HasVoted)
            return Result.Failure<CastVoteResponse>(VotingErrors.AlreadyVoted);

        // 5. Verify candidate exists in this election
        var candidate = await context.Candidates
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId && c.ElectionId == request.ElectionId);

        if (candidate is null)
            return Result.Failure<CastVoteResponse>(VotingErrors.CandidateNotFound);

        // 5b. For parliamentary elections, verify candidate is in voter's constituency
        if (election.Type == ElectionType.Parliamentary && candidate.ConstituencyId is not null)
        {
            var voter = await context.Voters
                .FirstOrDefaultAsync(v => v.Id == electionVoter.VoterId);
            
            if (voter?.ConstituencyId != candidate.ConstituencyId)
                return Result.Failure<CastVoteResponse>(VotingErrors.CandidateNotFound);
        }

        // 6. Encrypt the vote using hybrid encryption
        var (encryptedVote, encryptedAesKey, iv, authTag) = EncryptVote(request.CandidateId, election.PublicKey);

        // 7. Create ballot
        var ballot = new Ballot
        {
            ElectionId = request.ElectionId,
            ElectionVoterId = electionVoterId,
            EncryptedVote = encryptedVote,
            EncryptedAESKey = encryptedAesKey,
            IV = iv,
            AuthTag = authTag,
            CastAt = DateTime.UtcNow
        };

        context.Ballots.Add(ballot);

        // 8. Mark voter as voted
        electionVoter.HasVoted = true;
        electionVoter.VotedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        // 9. Generate receipt hash (proof of vote without revealing choice)
        var receiptHash = GenerateReceiptHash(ballot.Id, electionVoterId);

        return Result.Success(new CastVoteResponse(
            Success: true,
            Message: "Your vote has been cast successfully and encrypted.",
            ReceiptHash: receiptHash
        ));
    }

    private (byte[] EncryptedVote, byte[] EncryptedAesKey, byte[] IV, byte[] AuthTag) EncryptVote(int candidateId, string publicKeyPem)
    {
        // 1. Create vote payload
        var votePayload = JsonSerializer.Serialize(new { CandidateId = candidateId, Timestamp = DateTime.UtcNow });
        var voteBytes = Encoding.UTF8.GetBytes(votePayload);

        // 2. Generate random AES-256 key and IV
        var aesKey = RandomNumberGenerator.GetBytes(32); // 256 bits
        var iv = RandomNumberGenerator.GetBytes(12); // 96 bits for GCM

        // 3. Encrypt vote with AES-GCM
        var encryptedVote = new byte[voteBytes.Length];
        var authTag = new byte[16]; // 128-bit tag

        using (var aesGcm = new AesGcm(aesKey, 16))
        {
            aesGcm.Encrypt(iv, voteBytes, encryptedVote, authTag);
        }

        // 4. Encrypt AES key with RSA public key
        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        var encryptedAesKey = rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA256);

        return (encryptedVote, encryptedAesKey, iv, authTag);
    }

    private string GenerateReceiptHash(int ballotId, int electionVoterId)
    {
        var data = $"{ballotId}:{electionVoterId}:{DateTime.UtcNow.Ticks}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash)[..16]; // Short receipt
    }

    private static string? BuildLocationString(Voter voter)
    {
        var gov = voter.Governorate?.NameAr;
        var cons = voter.Constituency?.NameAr;

        if (gov is not null && cons is not null)
            return $"{gov} - {cons}";
        if (gov is not null)
            return gov;
        return null;
    }
}
