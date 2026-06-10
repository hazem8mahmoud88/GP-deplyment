namespace SecureVote.Entities;

public class Ballot
{
    public int Id { get; set; }
    public int ElectionId { get; set; }
    public int? ElectionVoterId { get; set; } // Nullable for anonymization after counting
    public byte[] EncryptedVote { get; set; } = Array.Empty<byte>(); // AES encrypted vote data
    public byte[] EncryptedAESKey { get; set; } = Array.Empty<byte>(); // RSA encrypted AES key
    public byte[] IV { get; set; } = Array.Empty<byte>(); // Initialization Vector (12 bytes for GCM)
    public byte[] AuthTag { get; set; } = Array.Empty<byte>(); // Authentication Tag (16 bytes)
    public DateTime CastAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Election Election { get; set; } = null!;
    public ElectionVoter? ElectionVoter { get; set; }
}
