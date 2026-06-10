using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using SecureVote.Contracts.Candidates;
using SecureVote.Extensions;
using SecureVote.Services;
using SecureVote.Constants;

namespace SecureVote.Controllers;

[ApiController]
[Route("api/elections/{electionId}/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.Organizer)]
public class CandidatesController(ICandidateService candidateService) : ControllerBase
{

    [HttpPost]
    public async Task<IActionResult> Create(int electionId, [FromBody] CreateCandidateRequest request)
    {
        var organizerId = GetOrganizerId();
        var result = await candidateService.CreateAsync(electionId, request, organizerId);

        if (result.IsFailure)
            return result.ToProblem();

        return CreatedAtAction(nameof(GetById), new { electionId, id = result.Value.Id }, result.Value);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(int electionId)
    {
        var result = await candidateService.GetPublicByElectionIdAsync(electionId);
        
        if (result.IsFailure)
            return result.ToProblem();

        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int electionId, int id)
    {
        var result = await candidateService.GetByIdAsync(id);

        if (result.IsFailure)
            return result.ToProblem();

        return Ok(result.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int electionId, int id, [FromBody] UpdateCandidateRequest request)
    {
        var organizerId = GetOrganizerId();
        var result = await candidateService.UpdateAsync(id, request, organizerId);

        if (result.IsFailure)
            return result.ToProblem();

        return Ok(result.Value);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int electionId, int id)
    {
        var organizerId = GetOrganizerId();
        var result = await candidateService.DeleteAsync(id, organizerId);

        if (result.IsFailure)
            return result.ToProblem();

        return NoContent();
    }

    [HttpPost("{id}/photo")]
    public async Task<IActionResult> UploadPhoto(int electionId, int id, IFormFile photo)
    {
        var organizerId = GetOrganizerId();
        var result = await candidateService.UploadPhotoAsync(id, photo, organizerId);

        if (result.IsFailure)
            return result.ToProblem();

        return Ok(result.Value);
    }

    private int GetOrganizerId()
    {
        var orgIdClaim = User.FindFirst("OrganizerId")?.Value;
        return int.TryParse(orgIdClaim, out var orgId) ? orgId : 0;
    }
}
