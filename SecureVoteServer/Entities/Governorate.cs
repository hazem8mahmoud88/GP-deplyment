namespace SecureVote.Entities;

public class Governorate
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    
    // Navigation properties
    public ICollection<Constituency> Constituencies { get; set; } = new List<Constituency>();
    public ICollection<Voter> Voters { get; set; } = new List<Voter>();
}
