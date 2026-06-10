using SecureVote.Abstractions;

namespace SecureVote.Errors;

public static class ElectionErrors
{
    public static readonly Error NotFound = new("Election.NotFound", "Election not found", StatusCodes.Status404NotFound);
    public static readonly Error AlreadyActive = new("Election.AlreadyActive", "Election is already active", StatusCodes.Status400BadRequest);
    public static readonly Error CannotModifyActive = new("Election.CannotModifyActive", "Cannot modify an active election", StatusCodes.Status400BadRequest);
    public static readonly Error CannotActivateWithoutCandidates = new("Election.NoCandidates", "Cannot activate election without candidates", StatusCodes.Status400BadRequest);
    public static readonly Error InvalidDateRange = new("Election.InvalidDateRange", "End date must be after start date", StatusCodes.Status400BadRequest);
    public static readonly Error AlreadyClosed = new("Election.AlreadyClosed", "Election is already closed", StatusCodes.Status400BadRequest);
    public static readonly Error CannotDeleteActive = new("Election.CannotDeleteActive", "Cannot delete an active election", StatusCodes.Status400BadRequest);
}
