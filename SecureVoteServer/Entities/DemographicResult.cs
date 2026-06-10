namespace SecureVote.Entities;

public class DemographicResult
{
    public int Id { get; set; }
    public int ElectionId { get; set; }
    public int CandidateId { get; set; }
    public string Category { get; set; } = string.Empty; // "Gender" or "AgeGroup"
    public string GroupName { get; set; } = string.Empty; // "Male"/"Female" or "18-25"/"26-35"/etc.
    public int VoteCount { get; set; }
    public decimal Percentage { get; set; }
    
    // Navigation properties
    public Election Election { get; set; } = null!;
    public Candidate Candidate { get; set; } = null!;
}
