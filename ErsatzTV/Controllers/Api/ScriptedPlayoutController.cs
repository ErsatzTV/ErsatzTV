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

    [HttpPost("add_marathon", Name = "AddMarathon")]
    public async Task<IActionResult> AddMarathon(
        [FromRoute]
        Guid buildId,
        string key,
        string groupBy,
        string itemOrder = "shuffle",
        Dictionary<string, List<string>> guids = null,
        List<string> searches = null,
        bool playAllItems = false,
        bool shuffleGroups = false)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        if (!Enum.TryParse(itemOrder, ignoreCase: true, out PlaybackOrder itemPlaybackOrder))
        {
            return BadRequest("Invalid item playback order.");
        }

        await engine.AddMarathon(
            key,
            guids ?? [],
            searches ?? [],
            groupBy,
            shuffleGroups,
            itemPlaybackOrder,
            playAllItems);
        return Ok();
    }

    [HttpPost("add_multi_collection", Name = "AddMultiCollection")]
    public async Task<IActionResult> AddMultiCollection(
        [FromRoute]
        Guid buildId,
        string key,
        string multiCollection,
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

        await engine.AddMultiCollection(key, multiCollection, playbackOrder, cancellationToken);
        return Ok();
    }

    [HttpPost("add_playlist", Name = "AddPlaylist")]
    public async Task<IActionResult> AddPlaylist(
        [FromRoute]
        Guid buildId,
        string key,
        string playlist,
        string playlistGroup,
        CancellationToken cancellationToken)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        await engine.AddPlaylist(key, playlist, playlistGroup, cancellationToken);
        return Ok();
    }

    [HttpPost("add_smart_collection", Name = "AddSmartCollection")]
    public async Task<IActionResult> AddSmartCollection(
        [FromRoute]
        Guid buildId,
        string key,
        string smartCollection,
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

        await engine.AddSmartCollection(key, smartCollection, playbackOrder, cancellationToken);
        return Ok();
    }

    [HttpPost("add_show", Name = "AddShow")]
    public async Task<IActionResult> AddShow(
        [FromRoute]
        Guid buildId,
        string key,
        Dictionary<string, string> guids,
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

        await engine.AddShow(key, guids, playbackOrder);
        return Ok();
    }

    [HttpPost("add_all", Name = "AddAll")]
    public IActionResult AddAll(
        [FromRoute]
        Guid buildId,
        string content,
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

        engine.AddAll(content, maybeFillerKind, customTitle, disableWatermarks);
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

    [HttpPost("add_duration", Name = "AddDuration")]
    public IActionResult AddDuration(
        [FromRoute]
        Guid buildId,
        string content,
        string duration,
        string fallback = null,
        bool trim = false,
        int discardAttempts = 0,
        bool stopBeforeEnd = true,
        bool offlineTail = false,
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

        engine.AddDuration(
            content,
            duration,
            fallback,
            trim,
            discardAttempts,
            stopBeforeEnd,
            offlineTail,
            maybeFillerKind,
            customTitle,
            disableWatermarks);
        return Ok();
    }

    [HttpPost("pad_to_next", Name = "PadToNext")]
    public IActionResult PadToNext(
        [FromRoute]
        Guid buildId,
        string content,
        int minutes,
        string fallback = null,
        bool trim = false,
        int discardAttempts = 0,
        bool stopBeforeEnd = true,
        bool offlineTail = true,
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

        engine.PadToNext(
            content,
            minutes,
            fallback,
            trim,
            discardAttempts,
            stopBeforeEnd,
            offlineTail,
            maybeFillerKind,
            customTitle,
            disableWatermarks);
        return Ok();
    }

    [HttpPost("pad_until", Name = "PadUntil")]
    public IActionResult PadUntil(
        [FromRoute]
        Guid buildId,
        string content,
        string when,
        bool tomorrow = false,
        string fallback = null,
        bool trim = false,
        int discardAttempts = 0,
        bool stopBeforeEnd = true,
        bool offlineTail = false,
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

        engine.PadUntil(
            content,
            when,
            tomorrow,
            fallback,
            trim,
            discardAttempts,
            stopBeforeEnd,
            offlineTail,
            maybeFillerKind,
            customTitle,
            disableWatermarks);
        return Ok();
    }
}
