using System.Globalization;
using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using SecureVote.Abstractions;
using SecureVote.Contracts.Organizer;
using SecureVote.Entities;
using SecureVote.Errors;
using SecureVote.Persistence;

namespace SecureVote.Services;

public class VoterUploadService(ApplicationDbContext context, ICloudinaryService cloudinaryService) : IVoterUploadService
{
    public async Task<Result<UploadVotersResponse>> UploadVotersCsvAsync(int electionId, IFormFile csvFile, int organizerId)
    {
        // Validate election exists and organizer is assigned
        var election = await context.Elections
            .Include(e => e.ElectionOrganizers)
            .FirstOrDefaultAsync(e => e.Id == electionId);

        if (election is null)
            return Result.Failure<UploadVotersResponse>(OrganizerErrors.ElectionNotFound);

        // Verify organizer is assigned to this election
        if (!election.ElectionOrganizers.Any(eo => eo.OrganizerId == organizerId))
            return Result.Failure<UploadVotersResponse>(OrganizerErrors.NotAssignedToElection);

        if (election.Status != ElectionStatus.Draft)
            return Result.Failure<UploadVotersResponse>(OrganizerErrors.ElectionNotActive);

        // Parse CSV file
        var errors = new List<string>();
        var newVoters = 0;
        var existingVotersAlreadyLinked = 0;
        var linkedExistingVoters = 0;
        var failedRows = 0;
        var rowNumber = 0;

        using var reader = new StreamReader(csvFile.OpenReadStream());
        
        // Read first line and check if it's a header or data
        var firstLine = await reader.ReadLineAsync();
        if (string.IsNullOrEmpty(firstLine))
            return Result.Failure<UploadVotersResponse>(OrganizerErrors.InvalidCsvFile);

        // If the first column starts with a digit, it's data (not a header)
        var isHeader = !char.IsDigit(firstLine.TrimStart().FirstOrDefault());
        
        // Create a queue of lines to process
        var linesToProcess = new List<string>();
        if (!isHeader)
            linesToProcess.Add(firstLine); // First line is data, don't skip it

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            linesToProcess.Add(line);
        }

        foreach (var currentLine in linesToProcess)
        {
            rowNumber++;
            if (string.IsNullOrWhiteSpace(currentLine)) continue;

            try
            {
                var values = ParseCsvLine(currentLine);
                if (values.Length < 1 || string.IsNullOrWhiteSpace(values[0]))
                {
                    failedRows++;
                    errors.Add($"Row {rowNumber}: Missing unique identifier");
                    continue;
                }

                var uniqueId = values[0].Trim();
                
                // Check if voter already exists
                var existingVoter = await context.Voters
                    .FirstOrDefaultAsync(v => v.UniqueIdentifier == uniqueId);

                Voter voter;
                if (existingVoter is not null)
                {
                    voter = existingVoter;

                    // Update geographic data if voter doesn't have it yet
                    if (voter.GovernorateId is null && values.Length > 6 && !string.IsNullOrWhiteSpace(values[6]))
                    {
                        var govName = values[6].Trim();
                        var governorate = await context.Governorates
                            .FirstOrDefaultAsync(g => g.NameAr == govName || g.NameEn == govName);
                        
                        if (governorate is not null)
                        {
                            voter.GovernorateId = governorate.Id;

                            if (values.Length > 7 && !string.IsNullOrWhiteSpace(values[7]))
                            {
                                var constName = values[7].Trim();
                                var constituency = await context.Constituencies
                                    .FirstOrDefaultAsync(c => c.GovernorateId == governorate.Id && 
                                        (c.NameAr == constName || c.NameEn == constName));
                                
                                if (constituency is not null)
                                    voter.ConstituencyId = constituency.Id;
                            }
                        }
                    }

                    // Check if already linked to this election
                    var alreadyLinked = await context.ElectionVoters
                        .AnyAsync(ev => ev.ElectionId == electionId && ev.VoterId == voter.Id);
                    
                    if (alreadyLinked)
                    {
                        existingVotersAlreadyLinked++;
                        continue;
                    }
                }
                else
                {
                    // Create new voter
                    voter = new Voter
                    {
                        UniqueIdentifier = uniqueId,
                        FullName = values.Length > 1 ? values[1].Trim() : null,
                        DateOfBirth = values.Length > 2 ? ParseDate(values[2]) : null,
                        Gender = values.Length > 3 ? values[3].Trim() : null,
                        PhoneNumber = values.Length > 4 ? values[4].Trim() : null,
                        Email = values.Length > 5 ? values[5].Trim() : null
                    };

                    // Parse Governorate (column 7) - lookup by Arabic or English name
                    if (values.Length > 6 && !string.IsNullOrWhiteSpace(values[6]))
                    {
                        var govName = values[6].Trim();
                        var governorate = await context.Governorates
                            .FirstOrDefaultAsync(g => g.NameAr == govName || g.NameEn == govName);
                        
                        if (governorate is not null)
                        {
                            voter.GovernorateId = governorate.Id;

                            // Parse Constituency (column 8) - lookup within the governorate
                            if (values.Length > 7 && !string.IsNullOrWhiteSpace(values[7]))
                            {
                                var constName = values[7].Trim();
                                var constituency = await context.Constituencies
                                    .FirstOrDefaultAsync(c => c.GovernorateId == governorate.Id && 
                                        (c.NameAr == constName || c.NameEn == constName));
                                
                                if (constituency is not null)
                                    voter.ConstituencyId = constituency.Id;
                            }
                        }
                    }

                    context.Voters.Add(voter);
                    newVoters++;
                }

                // Link voter to election
                var electionVoter = new ElectionVoter
                {
                    ElectionId = electionId,
                    Voter = voter,
                    IsEligible = true,
                    HasVoted = false
                };

                context.ElectionVoters.Add(electionVoter);
                if (existingVoter is not null)
                    linkedExistingVoters++;
            }
            catch (Exception ex)
            {
                failedRows++;
                errors.Add($"Row {rowNumber}: {ex.Message}");
            }
        }

        await context.SaveChangesAsync();

        if (newVoters == 0 && linkedExistingVoters == 0 && existingVotersAlreadyLinked == 0)
            return Result.Failure<UploadVotersResponse>(OrganizerErrors.NoVotersInCsv);

        return Result.Success(new UploadVotersResponse(
            TotalRows: rowNumber,
            NewVotersCreated: newVoters,
            ExistingVotersLinked: linkedExistingVoters + existingVotersAlreadyLinked,
            FailedRows: failedRows,
            Errors: errors.Take(10).ToList() // Limit errors to first 10
        ));
    }

    public async Task<Result<UploadPhotosResponse>> UploadPhotosZipAsync(int electionId, IFormFile zipFile, int organizerId)
    {
        // Validate election exists and organizer is assigned
        var election = await context.Elections
            .Include(e => e.ElectionOrganizers)
            .FirstOrDefaultAsync(e => e.Id == electionId);

        if (election is null)
            return Result.Failure<UploadPhotosResponse>(OrganizerErrors.ElectionNotFound);

        // Verify organizer is assigned to this election
        if (!election.ElectionOrganizers.Any(eo => eo.OrganizerId == organizerId))
            return Result.Failure<UploadPhotosResponse>(OrganizerErrors.NotAssignedToElection);

        if (election.Status != ElectionStatus.Draft)
            return Result.Failure<UploadPhotosResponse>(OrganizerErrors.ElectionNotActive);

        var totalPhotos = 0;
        var matchedPhotos = 0;
        var unmatchedFiles = new List<string>();

        try
        {
            using var stream = zipFile.OpenReadStream();
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name) || entry.Length == 0) continue;

                var extension = Path.GetExtension(entry.Name).ToLowerInvariant();
                if (extension != ".jpg" && extension != ".jpeg" && extension != ".png") continue;

                totalPhotos++;

                // Extract unique identifier from filename (without extension)
                var uniqueId = Path.GetFileNameWithoutExtension(entry.Name);

                // Find voter by unique identifier
                var voter = await context.Voters
                    .FirstOrDefaultAsync(v => v.UniqueIdentifier == uniqueId);

                if (voter is null)
                {
                    unmatchedFiles.Add(entry.Name);
                    continue;
                }

                // Upload photo to Cloudinary
                var fileName = $"{uniqueId}{extension}";
                using var entryStream = entry.Open();
                var cloudinaryUrl = await cloudinaryService.UploadImageAsync(
                    entryStream,
                    fileName,
                    $"voters/{electionId}"
                );

                // Update voter photo URL
                voter.PhotoUrl = cloudinaryUrl;
                matchedPhotos++;
            }

            await context.SaveChangesAsync();

            return Result.Success(new UploadPhotosResponse(
                TotalPhotos: totalPhotos,
                PhotosMatched: matchedPhotos,
                PhotosNotMatched: unmatchedFiles.Count,
                UnmatchedFiles: unmatchedFiles.Take(20).ToList()
            ));
        }
        catch
        {
            return Result.Failure<UploadPhotosResponse>(OrganizerErrors.InvalidZipFile);
        }
    }

    public async Task<Result<ElectionStatsResponse>> GetElectionStatsAsync(int electionId, int organizerId)
    {
        var election = await context.Elections
            .Include(e => e.ElectionOrganizers)
            .Include(e => e.Candidates)
            .Include(e => e.ElectionVoters)
            .FirstOrDefaultAsync(e => e.Id == electionId);

        if (election is null)
            return Result.Failure<ElectionStatsResponse>(OrganizerErrors.ElectionNotFound);

        // Verify organizer is assigned to this election
        if (!election.ElectionOrganizers.Any(eo => eo.OrganizerId == organizerId))
            return Result.Failure<ElectionStatsResponse>(OrganizerErrors.NotAssignedToElection);

        var totalVoters = election.ElectionVoters.Count;
        var eligibleVoters = election.ElectionVoters.Count(ev => ev.IsEligible);
        var votedCount = election.ElectionVoters.Count(ev => ev.HasVoted);
        var turnout = eligibleVoters > 0 ? (decimal)votedCount / eligibleVoters * 100 : 0;

        return Result.Success(new ElectionStatsResponse(
            ElectionId: election.Id,
            ElectionTitle: election.Title,
            Status: election.Status,
            TotalVoters: totalVoters,
            EligibleVoters: eligibleVoters,
            VotedCount: votedCount,
            TurnoutPercentage: Math.Round(turnout, 2),
            TotalCandidates: election.Candidates.Count,
            TotalOrganizers: election.ElectionOrganizers.Count,
            StartDate: election.StartDate,
            EndDate: election.EndDate
        ));
    }

    private static string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var inQuotes = false;
        var currentValue = "";

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(currentValue);
                currentValue = "";
            }
            else
            {
                currentValue += c;
            }
        }
        values.Add(currentValue);

        return values.ToArray();
    }

    private static DateTime? ParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        
        if (DateTime.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;
        
        return null;
    }
}
