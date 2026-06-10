namespace SecureVote.Errors;

public static class VotingErrors
{
    public static readonly Error ElectionNotFound = new(
        "Voting.ElectionNotFound",
        "Election not found",
        StatusCodes.Status404NotFound);

    public static readonly Error ElectionNotActive = new(
        "Voting.ElectionNotActive",
        "Election is not currently active for voting",
        StatusCodes.Status400BadRequest);

    public static readonly Error VoterNotFound = new(
        "Voting.VoterNotFound",
        "You are not registered to vote in this election",
        StatusCodes.Status404NotFound);

    public static readonly Error AlreadyVoted = new(
        "Voting.AlreadyVoted",
        "You have already cast your vote in this election",
        StatusCodes.Status400BadRequest);

    public static readonly Error FaceVerificationFailed = new(
        "Voting.FaceVerificationFailed",
        "Face verification failed. Please try again with a clearer photo",
        StatusCodes.Status401Unauthorized);

    public static readonly Error InvalidToken = new(
        "Voting.InvalidToken",
        "Invalid or expired voting token",
        StatusCodes.Status401Unauthorized);

    public static readonly Error CandidateNotFound = new(
        "Voting.CandidateNotFound",
        "Candidate not found in this election",
        StatusCodes.Status404NotFound);

    public static readonly Error VoterPhotoNotFound = new(
        "Voting.VoterPhotoNotFound",
        "Your photo is not registered. Please contact the election organizer",
        StatusCodes.Status400BadRequest);
}
