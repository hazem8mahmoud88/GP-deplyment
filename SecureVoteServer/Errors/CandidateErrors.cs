using SecureVote.Abstractions;

namespace SecureVote.Errors;

public static class CandidateErrors
{
    public static readonly Error NotFound = new("Candidate.NotFound", "Candidate not found", StatusCodes.Status404NotFound);
    public static readonly Error ElectionNotFound = new("Candidate.ElectionNotFound", "Election not found", StatusCodes.Status404NotFound);
    public static readonly Error ElectionNotDraft = new("Candidate.ElectionNotDraft", "Cannot modify candidates for non-draft elections", StatusCodes.Status400BadRequest);
    public static readonly Error DuplicateOrderNumber = new("Candidate.DuplicateOrder", "Order number already exists for this election", StatusCodes.Status400BadRequest);
    public static readonly Error NotAssignedToElection = new("Candidate.NotAssigned", "You are not assigned to this election", StatusCodes.Status403Forbidden);
    public static readonly Error InvalidPhotoFormat = new("Candidate.InvalidPhotoFormat", "Invalid photo format. Only JPG, JPEG, and PNG are allowed", StatusCodes.Status400BadRequest);
}
