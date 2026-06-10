using SecureVote.Abstractions;

namespace SecureVote.Errors;

public static class OrganizerErrors
{
    public static readonly Error ElectionNotFound = new("Organizer.ElectionNotFound", "Election not found", StatusCodes.Status404NotFound);
    public static readonly Error NotAssignedToElection = new("Organizer.NotAssigned", "You are not assigned to this election", StatusCodes.Status403Forbidden);
    public static readonly Error ElectionNotActive = new("Organizer.ElectionNotActive", "Election must be in Draft status for uploads", StatusCodes.Status400BadRequest);
    public static readonly Error InvalidCsvFile = new("Organizer.InvalidCsv", "Invalid CSV file format", StatusCodes.Status400BadRequest);
    public static readonly Error NoVotersInCsv = new("Organizer.EmptyCsv", "CSV file contains no valid voter records", StatusCodes.Status400BadRequest);
    public static readonly Error InvalidZipFile = new("Organizer.InvalidZip", "Invalid ZIP file", StatusCodes.Status400BadRequest);
}
