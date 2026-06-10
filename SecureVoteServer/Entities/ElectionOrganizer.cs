namespace SecureVote.Entities;

public class ElectionOrganizer
{
    public int Id { get; set; }
    public int ElectionId { get; set; }
    public int OrganizerId { get; set; }
    public int AssignedByAdminId { get; set; }
    public bool CanDecrypt { get; set; } = false;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Election Election { get; set; } = null!;
    public Organizer Organizer { get; set; } = null!;
    public Admin AssignedByAdmin { get; set; } = null!;
}
