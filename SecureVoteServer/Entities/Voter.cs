namespace SecureVote.Entities;

public class Voter
{
    public int Id { get; set; }
    public string UniqueIdentifier { get; set; } = string.Empty; // National ID or membership number
    public string? FullName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public int? GovernorateId { get; set; }
    public int? ConstituencyId { get; set; }
    public string? PhotoUrl { get; set; }
    public string? CustomData { get; set; } // JSON for additional fields
    
    // Navigation properties
    public Governorate? Governorate { get; set; }
    public Constituency? Constituency { get; set; }
    public ICollection<ElectionVoter> ElectionVoters { get; set; } = new List<ElectionVoter>();
}
