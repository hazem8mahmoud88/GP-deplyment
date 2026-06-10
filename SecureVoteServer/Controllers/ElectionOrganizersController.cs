namespace SecureVote.Controllers;

[ApiController]
[Route("api/elections/{electionId}/organizers")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.Admin)]
public class ElectionOrganizersController(IElectionOrganizerService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Assign(int electionId, [FromBody] AssignOrganizerRequest request)
    {
        var adminIdClaim = User.FindFirst("AdminId")?.Value;
        if (string.IsNullOrEmpty(adminIdClaim) || !int.TryParse(adminIdClaim, out var adminId))
        {
            return Unauthorized(new { error = "Admin ID not found in token" });
        }

        var result = await service.AssignAsync(electionId, request, adminId);
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return CreatedAtAction(nameof(GetAll), new { electionId }, result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int electionId)
    {
        var result = await service.GetByElectionIdAsync(electionId);
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return Ok(result.Value);
    }

    [HttpDelete("{organizerId}")]
    public async Task<IActionResult> Remove(int electionId, int organizerId)
    {
        var result = await service.RemoveAsync(electionId, organizerId);
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return NoContent();
    }

    [HttpGet("/api/admin/organizers")]
    public async Task<IActionResult> GetAllOrganizers()
    {
        var result = await service.GetAllOrganizersAsync();
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return Ok(result.Value);
    }
}
