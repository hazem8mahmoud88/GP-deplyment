namespace SecureVote.Authentication;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryInMinutes { get; set; } = 60;
    public int VotingTokenExpiryInMinutes { get; set; } = 5;
}
