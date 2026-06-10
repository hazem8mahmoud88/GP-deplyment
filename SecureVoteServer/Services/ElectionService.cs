using Microsoft.EntityFrameworkCore;
using SecureVote.Abstractions;
using SecureVote.Contracts.Elections;
using SecureVote.Entities;
using SecureVote.Errors;
using SecureVote.Persistence;
using SecureVote.Encryption;

namespace SecureVote.Services;

public class ElectionService(ApplicationDbContext context, IEncryptionService encryptionService) : IElectionService
{
    public async Task<Result<ElectionResponse>> CreateAsync(CreateElectionRequest request, int adminId)
    {
        // Generate RSA key pair for this election
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var publicKey = rsa.ExportRSAPublicKeyPem();
        var privateKey = rsa.ExportRSAPrivateKeyPem();
        
        // Encrypt private key with master key before storing
        var privateKeyEncrypted = encryptionService.Encrypt(privateKey);

        var election = new Election
        {
            Title = request.Title,
            Type = request.Type,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = ElectionStatus.Draft,
            PublicKey = publicKey,
            PrivateKeyEncrypted = privateKeyEncrypted,
            CreatedByAdminId = adminId,
            CreatedAt = DateTime.UtcNow
        };

        context.Elections.Add(election);
        await context.SaveChangesAsync();

        return Result.Success(MapToResponse(election));
    }

    public async Task<Result<IEnumerable<ElectionSummaryResponse>>> GetAllAsync()
    {
        var elections = await context.Elections
            .Include(e => e.Candidates)
            .Include(e => e.ElectionVoters)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        var response = elections.Select(e => new ElectionSummaryResponse(
            e.Id,
            e.Title,
            e.Type,
            e.Description,
            e.StartDate,
            e.EndDate,
            e.Status,
            e.Candidates.Count,
            e.ElectionVoters.Count
        ));

        return Result.Success(response);
    }

    public async Task<Result<ElectionResponse>> GetByIdAsync(int id)
    {
        var election = await context.Elections
            .Include(e => e.Candidates)
            .Include(e => e.ElectionOrganizers)
            .Include(e => e.ElectionVoters)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (election is null)
            return Result.Failure<ElectionResponse>(ElectionErrors.NotFound);

        return Result.Success(MapToResponse(election));
    }

    public async Task<Result<ElectionResponse>> UpdateAsync(int id, UpdateElectionRequest request)
    {
        var election = await context.Elections
            .Include(e => e.Candidates)
            .Include(e => e.ElectionOrganizers)
            .Include(e => e.ElectionVoters)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (election is null)
            return Result.Failure<ElectionResponse>(ElectionErrors.NotFound);

        if (election.Status == ElectionStatus.Active)
            return Result.Failure<ElectionResponse>(ElectionErrors.CannotModifyActive);

        if (election.Status == ElectionStatus.Closed)
            return Result.Failure<ElectionResponse>(ElectionErrors.AlreadyClosed);

        election.Title = request.Title;
        election.Type = request.Type;
        election.Description = request.Description;
        election.StartDate = request.StartDate;
        election.EndDate = request.EndDate;
        election.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Result.Success(MapToResponse(election));
    }

    public async Task<Result> ActivateAsync(int id)
    {
        var election = await context.Elections
            .Include(e => e.Candidates)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (election is null)
            return Result.Failure(ElectionErrors.NotFound);

        if (election.Status == ElectionStatus.Active)
            return Result.Failure(ElectionErrors.AlreadyActive);

        if (election.Status == ElectionStatus.Closed)
            return Result.Failure(ElectionErrors.AlreadyClosed);

        if (!election.Candidates.Any())
            return Result.Failure(ElectionErrors.CannotActivateWithoutCandidates);

        election.Status = ElectionStatus.Active;
        election.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> CloseAsync(int id)
    {
        var election = await context.Elections.FindAsync(id);

        if (election is null)
            return Result.Failure(ElectionErrors.NotFound);

        if (election.Status != ElectionStatus.Active)
            return Result.Failure(ElectionErrors.AlreadyClosed);

        election.Status = ElectionStatus.Closed;
        election.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var election = await context.Elections.FindAsync(id);

        if (election is null)
            return Result.Failure(ElectionErrors.NotFound);

        if (election.Status == ElectionStatus.Active)
            return Result.Failure(ElectionErrors.CannotDeleteActive);

        // Delete related data in correct FK order to avoid constraint errors

        var demoResults = context.DemographicResults.Where(dr => dr.ElectionId == id);
        context.DemographicResults.RemoveRange(demoResults);
        await context.SaveChangesAsync();

        var geoResults = context.GeographicResults.Where(gr => gr.ElectionId == id);
        context.GeographicResults.RemoveRange(geoResults);
        await context.SaveChangesAsync();

        var results = context.Results.Where(r => r.ElectionId == id);
        context.Results.RemoveRange(results);
        await context.SaveChangesAsync();

        var ballots = context.Ballots.Where(b => b.ElectionId == id);
        context.Ballots.RemoveRange(ballots);
        await context.SaveChangesAsync();

        var electionVoters = context.ElectionVoters.Where(ev => ev.ElectionId == id);
        context.ElectionVoters.RemoveRange(electionVoters);
        await context.SaveChangesAsync();

        var candidates = context.Candidates.Where(c => c.ElectionId == id);
        context.Candidates.RemoveRange(candidates);
        await context.SaveChangesAsync();

        var organizers = context.ElectionOrganizers.Where(eo => eo.ElectionId == id);
        context.ElectionOrganizers.RemoveRange(organizers);
        await context.SaveChangesAsync();

        context.Elections.Remove(election);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    private static ElectionResponse MapToResponse(Election election)
    {
        return new ElectionResponse(
            election.Id,
            election.Title,
            election.Type,
            election.Description,
            election.StartDate,
            election.EndDate,
            election.Status,
            election.PublicKey,
            election.CreatedByAdminId,
            election.CreatedAt,
            election.UpdatedAt,
            election.Candidates?.Count ?? 0,
            election.ElectionOrganizers?.Count ?? 0,
            election.ElectionVoters?.Count ?? 0
        );
    }

    public async Task<Result<IEnumerable<ElectionSummaryResponse>>> GetByOrganizerIdAsync(int organizerId)
    {
        var elections = await context.Elections
            .Include(e => e.Candidates)
            .Include(e => e.ElectionVoters)
            .Include(e => e.ElectionOrganizers)
            .Where(e => e.ElectionOrganizers.Any(eo => eo.OrganizerId == organizerId))
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        var response = elections.Select(e => new ElectionSummaryResponse(
            e.Id,
            e.Title,
            e.Type,
            e.Description,
            e.StartDate,
            e.EndDate,
            e.Status,
            e.Candidates.Count,
            e.ElectionVoters.Count
        ));

        return Result.Success(response);
    }
}
