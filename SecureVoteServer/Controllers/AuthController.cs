using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureVote.Contracts.Auth;
using SecureVote.Extensions;
using SecureVote.Services;

namespace SecureVote.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return Ok(result.Value);
    }

    [HttpPost("register/admin")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.Admin)]
    public async Task<IActionResult> RegisterAdmin([FromBody] RegisterAdminRequest request)
    {
        var result = await authService.RegisterAdminAsync(request);
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return CreatedAtAction(nameof(Login), result.Value);
    }

    [HttpPost("register/organizer")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.Admin)]
    public async Task<IActionResult> RegisterOrganizer([FromBody] RegisterOrganizerRequest request)
    {
        var result = await authService.RegisterOrganizerAsync(request);
        
        if (result.IsFailure)
            return result.ToProblem();
        
        return CreatedAtAction(nameof(Login), result.Value);
    }
}
