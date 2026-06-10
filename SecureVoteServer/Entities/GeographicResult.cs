namespace SecureVote.Entities;

public class GeographicResult
{
    public int Id { get; set; }
    public int ElectionId { get; set; }
    public int CandidateId { get; set; }
    public int GovernorateId { get; set; }
    public int? ConstituencyId { get; set; }
    public int VoteCount { get; set; }
    public decimal Percentage { get; set; }
    
    // Navigation properties
    public Election Election { get; set; } = null!;
    public Candidate Candidate { get; set; } = null!;
    public Governorate Governorate { get; set; } = null!;
    public Constituency? Constituency { get; set; }
}
