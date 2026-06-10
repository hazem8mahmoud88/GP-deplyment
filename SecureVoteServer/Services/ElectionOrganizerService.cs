using Microsoft.EntityFrameworkCore;
using SecureVote.Abstractions;
using SecureVote.Contracts.ElectionOrganizers;
using SecureVote.Entities;
using SecureVote.Errors;
using SecureVote.Persistence;

namespace SecureVote.Services;

public class ElectionOrganizerService(ApplicationDbContext context) : IElectionOrganizerService
{
    public async Task<Result<ElectionOrganizerResponse>> AssignAsync(int electionId, AssignOrganizerRequest request, int adminId)
    {
        var election = await context.Elections.FindAsync(electionId);
        if (election is null)
            return Result.Failure<ElectionOrganizerResponse>(ElectionOrganizerErrors.ElectionNotFound);

        var organizer = await context.Organizers
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == request.OrganizerId);
        
        if (organizer is null)
            return Result.Failure<ElectionOrganizerResponse>(ElectionOrganizerErrors.OrganizerNotFound);

        // Check if already assigned
        var exists = await context.ElectionOrganizers
            .AnyAsync(eo => eo.ElectionId == electionId && eo.OrganizerId == request.OrganizerId);
        
        if (exists)
            return Result.Failure<ElectionOrganizerResponse>(ElectionOrganizerErrors.AlreadyAssigned);

        var electionOrganizer = new ElectionOrganizer
        {
            ElectionId = electionId,
            OrganizerId = request.OrganizerId,
            AssignedByAdminId = adminId,
            CanDecrypt = request.CanDecrypt,
            AssignedAt = DateTime.UtcNow
        };

        context.ElectionOrganizers.Add(electionOrganizer);
        await context.SaveChangesAsync();

        return Result.Success(new ElectionOrganizerResponse(
            electionOrganizer.Id,
            electionOrganizer.ElectionId,
            electionOrganizer.OrganizerId,
            organizer.User.UserName ?? "",
            organizer.User.Email ?? "",
            electionOrganizer.AssignedByAdminId,
            electionOrganizer.CanDecrypt,
            electionOrganizer.AssignedAt
        ));
    }

    public async Task<Result<IEnumerable<ElectionOrganizerResponse>>> GetByElectionIdAsync(int electionId)
    {
        var election = await context.Elections.FindAsync(electionId);
        if (election is null)
            return Result.Failure<IEnumerable<ElectionOrganizerResponse>>(ElectionOrganizerErrors.ElectionNotFound);

        var organizers = await context.ElectionOrganizers
            .Where(eo => eo.ElectionId == electionId)
            .Include(eo => eo.Organizer)
                .ThenInclude(o => o.User)
            .ToListAsync();

        var response = organizers.Select(eo => new ElectionOrganizerResponse(
            eo.Id,
            eo.ElectionId,
            eo.OrganizerId,
            eo.Organizer.User.UserName ?? "",
            eo.Organizer.User.Email ?? "",
            eo.AssignedByAdminId,
            eo.CanDecrypt,
            eo.AssignedAt
        ));

        return Result.Success(response);
    }

    public async Task<Result> RemoveAsync(int electionId, int organizerId)
    {
        var electionOrganizer = await context.ElectionOrganizers
            .FirstOrDefaultAsync(eo => eo.ElectionId == electionId && eo.OrganizerId == organizerId);

        if (electionOrganizer is null)
            return Result.Failure(ElectionOrganizerErrors.NotAssigned);

        context.ElectionOrganizers.Remove(electionOrganizer);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<IEnumerable<OrganizerSummaryResponse>>> GetAllOrganizersAsync()
    {
        var organizers = await context.Organizers
            .Include(o => o.User)
            .ToListAsync();

        var response = organizers.Select(o => new OrganizerSummaryResponse(
            o.Id,
            o.FullName,
            o.User.Email ?? "",
            o.Organization,
            o.PhoneNumber
        ));

        return Result.Success(response);
    }
}
