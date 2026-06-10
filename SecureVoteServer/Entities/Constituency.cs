namespace SecureVote.Entities;

public class Constituency
{
    public int Id { get; set; }
    public int GovernorateId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    
    // Navigation properties
    public Governorate Governorate { get; set; } = null!;
    public ICollection<Voter> Voters { get; set; } = new List<Voter>();
}
