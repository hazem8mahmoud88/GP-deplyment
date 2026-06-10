using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureVote.Contracts.Voting;
using SecureVote.Extensions;
using SecureVote.Services;

namespace SecureVote.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VotingController(IVotingService votingService) : ControllerBase
{
  /// <summary>
  /// Step 1: Verify voter identity using National ID
  /// </summary>

  [HttpPost("verify-identity")]
  [AllowAnonymous]
  public async Task<IActionResult> VerifyIdentity([FromBody] VerifyIdentityRequest request)
  {
    var result = await votingService.VerifyIdentityAsync(request);

    if (result.IsFailure)
      return result.ToProblem();

    return Ok(result.Value);
  }

  /// <summary>
  /// Step 2: Verify voter face using selfie comparison
  /// Returns a short-lived voting token (5 minutes)
  /// </summary>

  [HttpPost("verify-face")]
  [AllowAnonymous]
  public async Task<IActionResult> VerifyFace([FromBody] VerifyFaceRequest request)
  {
    var result = await votingService.VerifyFaceAsync(request);

    if (result.IsFailure)
      return result.ToProblem();

    return Ok(result.Value);
  }

  /// <summary>
  /// Step 3: Cast encrypted vote using the voting token
  /// </summary>

  [HttpPost("cast-vote")]
  [AllowAnonymous]
  public async Task<IActionResult> CastVote([FromBody] CastVoteRequest request, [FromHeader(Name = "X-Voting-Token")] string votingToken)
  {
    if (string.IsNullOrEmpty(votingToken))
      return BadRequest(new { error = "Voting token is required in X-Voting-Token header" });

    var result = await votingService.CastVoteAsync(request, votingToken);

    if (result.IsFailure)
      return result.ToProblem();

    return Ok(result.Value);
  }
}
