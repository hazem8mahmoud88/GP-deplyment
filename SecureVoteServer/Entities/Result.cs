namespace SecureVote.Entities;

public class VoteResult
{
    public int Id { get; set; }
    public int ElectionId { get; set; }
    public int CandidateId { get; set; }
    public int VoteCount { get; set; }
    public decimal Percentage { get; set; }
    public int? CountedByOrganizerId { get; set; }
    public DateTime CountedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Election Election { get; set; } = null!;
    public Candidate Candidate { get; set; } = null!;
    public Organizer? CountedByOrganizer { get; set; }
}
