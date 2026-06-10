namespace SecureVote.Entities;

public class ElectionVoter
{
    public int Id { get; set; }
    public int ElectionId { get; set; }
    public int VoterId { get; set; }
    public bool IsEligible { get; set; } = true;
    public bool HasVoted { get; set; } = false;
    public DateTime? VotedAt { get; set; }
    
    // Navigation properties
    public Election Election { get; set; } = null!;
    public Voter Voter { get; set; } = null!;
    public ICollection<VotingSession> VotingSessions { get; set; } = new List<VotingSession>();
    public Ballot? Ballot { get; set; }
}
