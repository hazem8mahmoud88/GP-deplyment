namespace SecureVote.Entities;

public class Organizer : AuditableEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Organization { get; set; }
    public string? PhoneNumber { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<ElectionOrganizer> ElectionAssignments { get; set; } = new List<ElectionOrganizer>();
    public ICollection<VoteResult> CountedResults { get; set; } = new List<VoteResult>();
}
