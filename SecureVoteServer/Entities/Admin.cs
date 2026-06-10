namespace SecureVote.Entities;

public class Admin : AuditableEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? PhoneNumber { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Election> CreatedElections { get; set; } = new List<Election>();
    public ICollection<ElectionOrganizer> AssignedOrganizers { get; set; } = new List<ElectionOrganizer>();
}
