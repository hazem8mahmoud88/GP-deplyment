using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using SecureVote.Extensions;
using SecureVote.Services;

namespace SecureVote.Controllers;

[ApiController]
[Route("api/organizer")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.Organizer)]
public class OrganizerController(IVoterUploadService uploadService, IElectionService electionService) : ControllerBase
{
    [HttpGet("my-elections")]
    public async Task<IActionResult> GetMyElections()
    {
        var organizerId = GetOrganizerId();
        if (organizerId is null)
            return Unauthorized(new { error = "Organizer ID not found in token" });

        var result = await electionService.GetByOrganizerIdAsync(organizerId.Value);
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return Ok(result.Value);
    }

    [HttpPost("elections/{electionId}/upload-voters")]
    public async Task<IActionResult> UploadVoters(int electionId, IFormFile csvFile)
    {
        var organizerId = GetOrganizerId();
        if (organizerId is null)
            return Unauthorized(new { error = "Organizer ID not found in token" });

        var result = await uploadService.UploadVotersCsvAsync(electionId, csvFile, organizerId.Value);
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return Ok(result.Value);
    }

    [HttpPost("elections/{electionId}/upload-photos")]
    public async Task<IActionResult> UploadPhotos(int electionId, IFormFile zipFile)
    {
        var organizerId = GetOrganizerId();
        if (organizerId is null)
            return Unauthorized(new { error = "Organizer ID not found in token" });

        var result = await uploadService.UploadPhotosZipAsync(electionId, zipFile, organizerId.Value);
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return Ok(result.Value);
    }

    [HttpGet("elections/{electionId}/stats")]
    public async Task<IActionResult> GetStats(int electionId)
    {
        var organizerId = GetOrganizerId();
        if (organizerId is null)
            return Unauthorized(new { error = "Organizer ID not found in token" });

        var result = await uploadService.GetElectionStatsAsync(electionId, organizerId.Value);
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return Ok(result.Value);
    }

    private int? GetOrganizerId()
    {
        var orgIdClaim = User.FindFirst("OrganizerId")?.Value;
        return int.TryParse(orgIdClaim, out var orgId) ? orgId : null;
    }
}

