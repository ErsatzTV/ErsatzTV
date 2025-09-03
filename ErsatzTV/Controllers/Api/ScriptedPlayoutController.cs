using ErsatzTV.Core.Api.ScriptedPlayout;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.Engine;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[EndpointGroupName("scripted-playout")]
[Route("api/scripted/playout/build/{buildId:guid}")]
public class ScriptedPlayoutController(IScriptedPlayoutBuilderService scriptedPlayoutBuilderService) : ControllerBase
{
    [HttpGet("context", Name="GetContext")]
    public ActionResult<ContextResponseModel> GetContext([FromRoute]Guid buildId)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        var state = engine.GetState();

        // TODO: better IsDone logic
        return Ok(
            new ContextResponseModel(
                state.CurrentTime,
                state.Start,
                state.Finish,
                state.CurrentTime >= state.Finish));
    }

    [HttpPost("add_collection", Name = "AddCollection")]
    public async Task<IActionResult> AddCollection(
        [FromRoute]
        Guid buildId,
        string key,
        string collection,
        string order,
        CancellationToken cancellationToken)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        if (!Enum.TryParse(order, ignoreCase: true, out PlaybackOrder playbackOrder))
        {
            return BadRequest("Invalid playback order.");
        }

        await engine.AddCollection(key, collection, playbackOrder, cancellationToken);
        return Ok();
    }

    [HttpPost("add_count", Name = "AddCount")]
    public IActionResult AddCount(
        [FromRoute]
        Guid buildId,
        string content,
        int count,
        string fillerKind = null,
        string customTitle = null,
        bool disableWatermarks = false)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        Option<FillerKind> maybeFillerKind = Option<FillerKind>.None;
        if (Enum.TryParse(fillerKind, ignoreCase: true, out FillerKind fk))
        {
            maybeFillerKind = fk;
        }

        engine.AddCount(content, count, maybeFillerKind, customTitle, disableWatermarks);
        return Ok();
    }
}
