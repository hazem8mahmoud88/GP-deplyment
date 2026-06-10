using Microsoft.AspNetCore.Identity;

namespace SecureVote.Entities;

public class User : IdentityUser
{
    public string Role { get; set; } = string.Empty; // Admin or Organizer
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Admin? Admin { get; set; }
    public Organizer? Organizer { get; set; }
}
