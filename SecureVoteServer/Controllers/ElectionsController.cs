namespace SecureVote.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.Admin)]
public class ElectionsController(IElectionService electionService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateElectionRequest request)
    {
        var adminIdClaim = User.FindFirst("AdminId")?.Value;
        if (string.IsNullOrEmpty(adminIdClaim) || !int.TryParse(adminIdClaim, out var adminId))
        {
            return Unauthorized(new { error = "Admin ID not found in token" });
        }

        var result = await electionService.CreateAsync(request, adminId);
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var result = await electionService.GetAllAsync();
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
      {
          var result = await electionService.GetByIdAsync(id);
        
          if (result.IsFailure)
              return result.ToProblem();
        
          return Ok(result.Value);
      }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateElectionRequest request)
    {
        var result = await electionService.UpdateAsync(id, request);
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return Ok(result.Value);
    }

    [HttpPut("{id}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var result = await electionService.ActivateAsync(id);
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return NoContent();
    }

    [HttpPut("{id}/close")]
    public async Task<IActionResult> Close(int id)
    {
        var result = await electionService.CloseAsync(id);
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await electionService.DeleteAsync(id);
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return NoContent();
    }
}
