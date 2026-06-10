namespace SecureVote.Constants;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Organizer = "Organizer";
    
    // Combined roles for authorization
    public const string AdminOrOrganizer = $"{Admin},{Organizer}";
}
