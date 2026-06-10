namespace SecureVote.Contracts.Organizer;

// ========== Request DTOs ==========

public record UploadVotersRequest(
    IFormFile CsvFile
);

public record UploadPhotosRequest(
    IFormFile ZipFile
);

// ========== Response DTOs ==========

public record UploadVotersResponse(
    int TotalRows,
    int NewVotersCreated,
    int ExistingVotersLinked,
    int FailedRows,
    List<string> Errors
);

public record UploadPhotosResponse(
    int TotalPhotos,
    int PhotosMatched,
    int PhotosNotMatched,
    List<string> UnmatchedFiles
);

public record ElectionStatsResponse(
    int ElectionId,
    string ElectionTitle,
    string Status,
    int TotalVoters,
    int EligibleVoters,
    int VotedCount,
    decimal TurnoutPercentage,
    int TotalCandidates,
    int TotalOrganizers,
    DateTime StartDate,
    DateTime EndDate
);

// ========== CSV Row Model ==========

public record VoterCsvRow
{
    public string UniqueIdentifier { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Governorate { get; set; }
    public string? Constituency { get; set; }
}
