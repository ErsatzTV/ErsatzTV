using ErsatzTV.Core.Api.ScriptedPlayout;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.Engine;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[Route("api/scripted/playout/build/{buildId:guid}")]
public class ScriptedPlayoutController(IScriptedPlayoutBuilderService scriptedPlayoutBuilderService, IServiceProvider serviceProvider) : ControllerBase
{
    [HttpPost]
    [Route("mock")]
    public IActionResult Start(Guid buildId)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine is not null)
        {
            return BadRequest("Build id is already in use");
        }

        engine = serviceProvider.GetService<ISchedulingEngine>();
        if (!scriptedPlayoutBuilderService.MockSession(engine, buildId))
        {
            return BadRequest("Build id is already in use");
        }

        return Ok();
    }

    [HttpGet("context")]
    public IActionResult GetContext([FromRoute]Guid buildId)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        var state = engine.GetState();

        // TODO: better IsDone
        return Ok(
            new ContextResponseModel(
                state.CurrentTime,
                state.Start,
                state.Finish,
                state.CurrentTime >= state.Finish));
    }

    [HttpPost("ping")]
    public IActionResult Ping([FromRoute]Guid buildId)
    {
        var engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            Console.WriteLine($"No active engine for {buildId}");
            return NotFound();
        }

        Console.WriteLine($"Ping playout build {buildId}");
        return Ok();
    }
}
