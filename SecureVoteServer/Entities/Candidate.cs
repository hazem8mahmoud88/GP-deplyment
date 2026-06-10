namespace SecureVote.Entities;

public class Candidate
{
    public int Id { get; set; }
    public int ElectionId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public string? PartyName { get; set; }
    public string? PhotoPath { get; set; }
    public int OrderNumber { get; set; }
    public int? ConstituencyId { get; set; } // null = presidential/general, set = parliamentary (specific district)
    
    // Navigation properties
    public Election Election { get; set; } = null!;
    public Constituency? Constituency { get; set; }
    public ICollection<VoteResult> Results { get; set; } = new List<VoteResult>();
}
