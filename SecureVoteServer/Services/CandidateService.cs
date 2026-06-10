namespace SecureVote.Services;

public class CandidateService(ApplicationDbContext context) : ICandidateService
{
    public async Task<Result<CandidateResponse>> CreateAsync(int electionId, CreateCandidateRequest request, int organizerId)
    {
        var election = await context.Elections
            .Include(e => e.ElectionOrganizers)
            .FirstOrDefaultAsync(e => e.Id == electionId);
        
        if (election is null)
            return Result.Failure<CandidateResponse>(CandidateErrors.ElectionNotFound);

        // Verify organizer is assigned to this election
        if (!election.ElectionOrganizers.Any(eo => eo.OrganizerId == organizerId))
            return Result.Failure<CandidateResponse>(CandidateErrors.NotAssignedToElection);

        if (election.Status != ElectionStatus.Draft)
            return Result.Failure<CandidateResponse>(CandidateErrors.ElectionNotDraft);

        // Check for duplicate order number
        var orderExists = await context.Candidates
            .AnyAsync(c => c.ElectionId == electionId && c.OrderNumber == request.OrderNumber);
        
        if (orderExists)
            return Result.Failure<CandidateResponse>(CandidateErrors.DuplicateOrderNumber);

        var candidate = new Candidate
        {
            ElectionId = electionId,
            FullName = request.FullName,
            Symbol = request.Symbol,
            PartyName = request.PartyName,
            OrderNumber = request.OrderNumber,
            ConstituencyId = request.ConstituencyId
        };

        context.Candidates.Add(candidate);
        await context.SaveChangesAsync();

        // Load constituency name for response
        if (candidate.ConstituencyId is not null)
            await context.Entry(candidate).Reference(c => c.Constituency).LoadAsync();

        return Result.Success(MapToResponse(candidate));
    }

    public async Task<Result<IEnumerable<CandidateResponse>>> GetByElectionIdAsync(int electionId, int organizerId)
    {
        var election = await context.Elections
            .Include(e => e.ElectionOrganizers)
            .FirstOrDefaultAsync(e => e.Id == electionId);
        
        if (election is null)
            return Result.Failure<IEnumerable<CandidateResponse>>(CandidateErrors.ElectionNotFound);

        // Verify organizer is assigned to this election
        if (!election.ElectionOrganizers.Any(eo => eo.OrganizerId == organizerId))
            return Result.Failure<IEnumerable<CandidateResponse>>(CandidateErrors.NotAssignedToElection);

        var candidates = await context.Candidates
            .Include(c => c.Constituency)
            .Where(c => c.ElectionId == electionId)
            .OrderBy(c => c.OrderNumber)
            .ToListAsync();

        return Result.Success(candidates.Select(MapToResponse));
    }

    public async Task<Result<IEnumerable<CandidateResponse>>> GetPublicByElectionIdAsync(int electionId)
    {
        var electionExists = await context.Elections.AnyAsync(e => e.Id == electionId);
        if (!electionExists)
            return Result.Failure<IEnumerable<CandidateResponse>>(CandidateErrors.ElectionNotFound);

        var candidates = await context.Candidates
            .Include(c => c.Constituency)
            .Where(c => c.ElectionId == electionId)
            .OrderBy(c => c.OrderNumber)
            .ToListAsync();

        return Result.Success(candidates.Select(MapToResponse));
    }

    public async Task<Result<CandidateResponse>> GetByIdAsync(int id)
    {
        var candidate = await context.Candidates
            .Include(c => c.Constituency)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (candidate is null)
            return Result.Failure<CandidateResponse>(CandidateErrors.NotFound);

        return Result.Success(MapToResponse(candidate));
    }

    public async Task<Result<CandidateResponse>> UpdateAsync(int id, UpdateCandidateRequest request, int organizerId)
    {
        var candidate = await context.Candidates
            .Include(c => c.Election)
                .ThenInclude(e => e.ElectionOrganizers)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (candidate is null)
            return Result.Failure<CandidateResponse>(CandidateErrors.NotFound);

        // Verify organizer is assigned to this election
        if (!candidate.Election.ElectionOrganizers.Any(eo => eo.OrganizerId == organizerId))
            return Result.Failure<CandidateResponse>(CandidateErrors.NotAssignedToElection);

        if (candidate.Election.Status != ElectionStatus.Draft)
            return Result.Failure<CandidateResponse>(CandidateErrors.ElectionNotDraft);

        // Check for duplicate order number (excluding current candidate)
        var orderExists = await context.Candidates
            .AnyAsync(c => c.ElectionId == candidate.ElectionId && c.OrderNumber == request.OrderNumber && c.Id != id);
        
        if (orderExists)
            return Result.Failure<CandidateResponse>(CandidateErrors.DuplicateOrderNumber);

        candidate.FullName = request.FullName;
        candidate.Symbol = request.Symbol;
        candidate.PartyName = request.PartyName;
        candidate.OrderNumber = request.OrderNumber;
        candidate.ConstituencyId = request.ConstituencyId;

        await context.SaveChangesAsync();

        // Load constituency name for response
        if (candidate.ConstituencyId is not null)
            await context.Entry(candidate).Reference(c => c.Constituency).LoadAsync();

        return Result.Success(MapToResponse(candidate));
    }

    public async Task<Result> DeleteAsync(int id, int organizerId)
    {
        var candidate = await context.Candidates
            .Include(c => c.Election)
                .ThenInclude(e => e.ElectionOrganizers)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (candidate is null)
            return Result.Failure(CandidateErrors.NotFound);

        // Verify organizer is assigned to this election
        if (!candidate.Election.ElectionOrganizers.Any(eo => eo.OrganizerId == organizerId))
            return Result.Failure(CandidateErrors.NotAssignedToElection);

        if (candidate.Election.Status != ElectionStatus.Draft)
            return Result.Failure(CandidateErrors.ElectionNotDraft);

        // Delete photo file from disk if exists
        if (!string.IsNullOrEmpty(candidate.PhotoPath))
        {
            var photoFullPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", candidate.PhotoPath.TrimStart('/').Replace("/uploads/", ""));
            if (File.Exists(photoFullPath))
                File.Delete(photoFullPath);
        }

        context.Candidates.Remove(candidate);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<CandidateResponse>> UploadPhotoAsync(int id, IFormFile photo, int organizerId)
    {
        var candidate = await context.Candidates
            .Include(c => c.Election)
                .ThenInclude(e => e.ElectionOrganizers)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (candidate is null)
            return Result.Failure<CandidateResponse>(CandidateErrors.NotFound);

        // Verify organizer is assigned to this election
        if (!candidate.Election.ElectionOrganizers.Any(eo => eo.OrganizerId == organizerId))
            return Result.Failure<CandidateResponse>(CandidateErrors.NotAssignedToElection);


        // Validate file extension
        var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
        if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
            return Result.Failure<CandidateResponse>(CandidateErrors.InvalidPhotoFormat);

        // Delete old photo if exists (handles extension change: jpg → png)
        if (!string.IsNullOrEmpty(candidate.PhotoPath))
        {
            var oldPhotoPath = Path.Combine(Directory.GetCurrentDirectory(), candidate.PhotoPath.TrimStart('/'));
            if (File.Exists(oldPhotoPath))
                File.Delete(oldPhotoPath);
        }

        // Create directory if not exists
        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", "candidates", candidate.ElectionId.ToString());
        Directory.CreateDirectory(uploadsPath);

        // Save file with candidate ID as filename
        var fileName = $"{candidate.Id}{extension}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await photo.CopyToAsync(stream);
        }

        // Update candidate photo path
        candidate.PhotoPath = $"/uploads/candidates/{candidate.ElectionId}/{fileName}";
        await context.SaveChangesAsync();

        return Result.Success(MapToResponse(candidate));
    }

    private static CandidateResponse MapToResponse(Candidate candidate)
    {
        return new CandidateResponse(
            candidate.Id,
            candidate.ElectionId,
            candidate.FullName,
            candidate.Symbol,
            candidate.PartyName,
            candidate.PhotoPath,
            candidate.OrderNumber,
            candidate.ConstituencyId,
            candidate.Constituency?.NameAr
        );
    }
}

