using MARS.Scoreboard.Entities;
using MARS.Scoreboard.Services;
using MARS.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace MARS.Scoreboard.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScoreboardController(ScoreboardService scoreboardService, ILogger<ScoreboardController> logger)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OperationResult<ScoreboardDto>>> GetCurrentState(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var state = await scoreboardService.GetCurrentStateAsync();
            return Ok(OperationResult<ScoreboardDto>.Ok(state));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting scoreboard state");
            return Ok(OperationResult<ScoreboardDto>.Fail("Error getting scoreboard state"));
        }
    }

    [HttpPut]
    public async Task<ActionResult<OperationResult>> UpdateState(
        [FromBody] ScoreboardDto state,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await scoreboardService.UpdateStateAsync(state);
            return Ok(OperationResult.Ok());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating scoreboard state");
            return Ok(OperationResult.Fail("Error updating scoreboard state"));
        }
    }

    [HttpPatch("visibility")]
    public async Task<ActionResult<OperationResult>> SetVisibility(
        [FromBody] bool isVisible,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var success = await scoreboardService.SetVisibilityAsync(isVisible);
            return success
                ? Ok(OperationResult.Ok())
                : Ok(OperationResult.Fail("Failed to set visibility"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting scoreboard visibility");
            return Ok(OperationResult.Fail("Error setting visibility"));
        }
    }

    [HttpPatch("player/{position}/score")]
    public async Task<ActionResult<OperationResult>> UpdatePlayerScore(
        int position,
        [FromBody] int score,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var success = await scoreboardService.UpdatePlayerScoreAsync(position, score);
            return success
                ? Ok(OperationResult.Ok())
                : Ok(OperationResult.Fail("Failed to update player score"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating player score");
            return Ok(OperationResult.Fail("Error updating player score"));
        }
    }

    [HttpPatch("player/{position}/final")]
    public async Task<ActionResult<OperationResult>> SetPlayerFinal(
        int position,
        [FromBody] string final,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var success = await scoreboardService.SetPlayerFinalAsync(position, final);
            return success
                ? Ok(OperationResult.Ok())
                : Ok(OperationResult.Fail("Failed to set player final"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting player final");
            return Ok(OperationResult.Fail("Error setting player final"));
        }
    }
}
