namespace SecureVote.Errors;

public static class ResultsErrors
{
    public static readonly Error ElectionNotFound = new(
        "Results.ElectionNotFound",
        "Election not found",
        StatusCodes.Status404NotFound);

    public static readonly Error ElectionNotClosed = new(
        "Results.ElectionNotClosed",
        "Cannot count votes. Election must be closed first",
        StatusCodes.Status400BadRequest);

    public static readonly Error NotAuthorized = new(
        "Results.NotAuthorized",
        "You are not authorized to count votes for this election",
        StatusCodes.Status403Forbidden);

    public static readonly Error CannotDecrypt = new(
        "Results.CannotDecrypt",
        "You do not have permission to decrypt votes for this election",
        StatusCodes.Status403Forbidden);

    public static readonly Error AlreadyCounted = new(
        "Results.AlreadyCounted",
        "Votes for this election have already been counted",
        StatusCodes.Status400BadRequest);

    public static readonly Error NoBallots = new(
        "Results.NoBallots",
        "No votes have been cast in this election",
        StatusCodes.Status400BadRequest);

    public static readonly Error DecryptionFailed = new(
        "Results.DecryptionFailed",
        "Failed to decrypt election private key",
        StatusCodes.Status500InternalServerError);

    public static readonly Error ResultsNotAvailable = new(
        "Results.ResultsNotAvailable",
        "Results are not available yet. Votes have not been counted",
        StatusCodes.Status404NotFound);
}
