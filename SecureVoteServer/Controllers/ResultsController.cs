using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using SecureVote.Constants;
using SecureVote.Extensions;
using SecureVote.Services;

namespace SecureVote.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResultsController(IResultsService resultsService) : ControllerBase
{
    /// <summary>
    /// Count votes for a closed election.
    /// Requires Organizer role with CanDecrypt permission.
    /// This permanently anonymizes ballots by removing voter links.
    /// </summary>
    [HttpPost("count/{electionId}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.Organizer)]
    public async Task<IActionResult> CountVotes(int electionId)
    {
        var organizerId = GetOrganizerId();
        if (organizerId is null)
            return Unauthorized(new { error = "Organizer ID not found in token" });

        var result = await resultsService.CountVotesAsync(electionId, organizerId.Value);
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return Ok(result.Value);
    }

    /// <summary>
    /// Get election results after counting.
    /// Public access - anyone can view results after they are published.
    /// </summary>
    [HttpGet("{electionId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetResults(int electionId)
    {
        var result = await resultsService.GetResultsAsync(electionId);
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return Ok(result.Value);
    }

    /// <summary>
    /// Get vote results broken down by governorate.
    /// </summary>
    [HttpGet("{electionId}/by-governorate")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStatsByGovernorate(int electionId)
    {
        var result = await resultsService.GetStatsByGovernorateAsync(electionId);
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return Ok(result.Value);
    }

    /// <summary>
    /// Get vote results broken down by constituency within a governorate.
    /// </summary>
    [HttpGet("{electionId}/by-constituency/{governorateId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStatsByConstituency(int electionId, int governorateId)
    {
        var result = await resultsService.GetStatsByConstituencyAsync(electionId, governorateId);
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return Ok(result.Value);
    }

    /// <summary>
    /// Get voter participation statistics by governorate.
    /// </summary>
    [HttpGet("{electionId}/participation")]
    [AllowAnonymous]
    public async Task<IActionResult> GetParticipationStats(int electionId)
    {
        var result = await resultsService.GetParticipationStatsAsync(electionId);
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return Ok(result.Value);
    }

    /// <summary>
    /// Get demographic vote breakdown (gender and age) per candidate.
    /// </summary>
    [HttpGet("{electionId}/demographics")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDemographicStats(int electionId)
    {
        var result = await resultsService.GetDemographicStatsAsync(electionId);
        
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
