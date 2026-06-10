namespace SecureVote.Entities;

public class VotingSession
{
    public int Id { get; set; }
    public int ElectionVoterId { get; set; }
    public string Token { get; set; } = string.Empty; // JWT token
    public bool FaceVerified { get; set; } = false;
    public DateTime? FaceVerifiedAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime ExpiresAt { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ElectionVoter ElectionVoter { get; set; } = null!;
}
