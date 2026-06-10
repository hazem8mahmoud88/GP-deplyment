namespace SecureVote.Entities;

public class Election : AuditableEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Presidential, Union, Student, etc.
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = Constants.ElectionStatus.Draft; // Draft, Active, Closed
    public string PublicKey { get; set; } = string.Empty; // RSA Public Key (PEM)
    public string PrivateKeyEncrypted { get; set; } = string.Empty; // RSA Private Key (encrypted with master key)
    public int CreatedByAdminId { get; set; }
    
    // Navigation properties
    public Admin CreatedByAdmin { get; set; } = null!;
    public ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();
    public ICollection<ElectionOrganizer> ElectionOrganizers { get; set; } = new List<ElectionOrganizer>();
    public ICollection<ElectionVoter> ElectionVoters { get; set; } = new List<ElectionVoter>();
    public ICollection<Ballot> Ballots { get; set; } = new List<Ballot>();
    public ICollection<VoteResult> Results { get; set; } = new List<VoteResult>();
}
