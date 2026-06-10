using SecureVote.Abstractions;

namespace SecureVote.Errors;

public static class ElectionOrganizerErrors
{
    public static readonly Error ElectionNotFound = new("ElectionOrganizer.ElectionNotFound", "Election not found", StatusCodes.Status404NotFound);
    public static readonly Error OrganizerNotFound = new("ElectionOrganizer.OrganizerNotFound", "Organizer not found", StatusCodes.Status404NotFound);
    public static readonly Error AlreadyAssigned = new("ElectionOrganizer.AlreadyAssigned", "Organizer is already assigned to this election", StatusCodes.Status400BadRequest);
    public static readonly Error NotAssigned = new("ElectionOrganizer.NotAssigned", "Organizer is not assigned to this election", StatusCodes.Status404NotFound);
}
