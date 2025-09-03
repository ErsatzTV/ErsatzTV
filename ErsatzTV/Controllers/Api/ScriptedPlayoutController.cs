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
    [HttpGet("context", Name = "GetContext")]
    public ActionResult<ContextResponseModel> GetContext([FromRoute] Guid buildId)
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
        [FromBody]
        AddCollectionRequestModel request,
        CancellationToken cancellationToken)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        if (!Enum.TryParse(request.Order, ignoreCase: true, out PlaybackOrder playbackOrder))
        {
            return BadRequest("Invalid playback order.");
        }

        await engine.AddCollection(request.Key, request.Collection, playbackOrder, cancellationToken);
        return Ok();
    }

    [HttpPost("add_marathon", Name = "AddMarathon")]
    public async Task<IActionResult> AddMarathon([FromRoute] Guid buildId, [FromBody] AddMarathonRequestModel request)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        if (!Enum.TryParse(request.ItemOrder, ignoreCase: true, out PlaybackOrder itemPlaybackOrder))
        {
            return BadRequest("Invalid item playback order.");
        }

        await engine.AddMarathon(
            request.Key,
            request.Guids ?? [],
            request.Searches ?? [],
            request.GroupBy,
            request.ShuffleGroups,
            itemPlaybackOrder,
            request.PlayAllItems);
        return Ok();
    }

    [HttpPost("add_multi_collection", Name = "AddMultiCollection")]
    public async Task<IActionResult> AddMultiCollection(
        [FromRoute]
        Guid buildId,
        [FromBody]
        AddMultiCollectionRequestModel request,
        CancellationToken cancellationToken)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        if (!Enum.TryParse(request.Order, ignoreCase: true, out PlaybackOrder playbackOrder))
        {
            return BadRequest("Invalid playback order.");
        }

        await engine.AddMultiCollection(request.Key, request.MultiCollection, playbackOrder, cancellationToken);
        return Ok();
    }

    [HttpPost("add_playlist", Name = "AddPlaylist")]
    public async Task<IActionResult> AddPlaylist(
        [FromRoute]
        Guid buildId,
        [FromBody]
        AddPlaylistRequestModel request,
        CancellationToken cancellationToken)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        await engine.AddPlaylist(request.Key, request.Playlist, request.PlaylistGroup, cancellationToken);
        return Ok();
    }

    [HttpPost("add_smart_collection", Name = "AddSmartCollection")]
    public async Task<IActionResult> AddSmartCollection(
        [FromRoute]
        Guid buildId,
        [FromBody]
        AddSmartCollectionRequestModel request,
        CancellationToken cancellationToken)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        if (!Enum.TryParse(request.Order, ignoreCase: true, out PlaybackOrder playbackOrder))
        {
            return BadRequest("Invalid playback order.");
        }

        await engine.AddSmartCollection(request.Key, request.SmartCollection, playbackOrder, cancellationToken);
        return Ok();
    }

    [HttpPost("add_show", Name = "AddShow")]
    public async Task<IActionResult> AddShow(
        [FromRoute]
        Guid buildId,
        [FromBody]
        AddShowRequestModel request,
        CancellationToken cancellationToken)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        if (!Enum.TryParse(request.Order, ignoreCase: true, out PlaybackOrder playbackOrder))
        {
            return BadRequest("Invalid playback order.");
        }

        await engine.AddShow(request.Key, request.Guids, playbackOrder);
        return Ok();
    }

    [HttpPost("add_all", Name = "AddAll")]
    public IActionResult AddAll([FromRoute] Guid buildId, [FromBody] AddAllRequestModel request)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        Option<FillerKind> maybeFillerKind = Option<FillerKind>.None;
        if (Enum.TryParse(request.FillerKind, ignoreCase: true, out FillerKind fk))
        {
            maybeFillerKind = fk;
        }

        engine.AddAll(request.Content, maybeFillerKind, request.CustomTitle, request.DisableWatermarks);
        return Ok();
    }

    [HttpPost("add_count", Name = "AddCount")]
    public IActionResult AddCount(
        [FromRoute]
        Guid buildId,
        [FromBody]
        AddCountRequestModel request)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        Option<FillerKind> maybeFillerKind = Option<FillerKind>.None;
        if (Enum.TryParse(request.FillerKind, ignoreCase: true, out FillerKind fk))
        {
            maybeFillerKind = fk;
        }

        engine.AddCount(
            request.Content,
            request.Count,
            maybeFillerKind,
            request.CustomTitle,
            request.DisableWatermarks);
        return Ok();
    }

    [HttpPost("add_duration", Name = "AddDuration")]
    public IActionResult AddDuration([FromRoute] Guid buildId, [FromBody] AddDurationRequestModel request)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        Option<FillerKind> maybeFillerKind = Option<FillerKind>.None;
        if (Enum.TryParse(request.FillerKind, ignoreCase: true, out FillerKind fk))
        {
            maybeFillerKind = fk;
        }

        engine.AddDuration(
            request.Content,
            request.Duration,
            request.Fallback,
            request.Trim,
            request.DiscardAttempts,
            request.StopBeforeEnd,
            request.OfflineTail,
            maybeFillerKind,
            request.CustomTitle,
            request.DisableWatermarks);
        return Ok();
    }

    [HttpPost("pad_to_next", Name = "PadToNext")]
    public IActionResult PadToNext([FromRoute] Guid buildId, [FromBody] PadToNextRequestModel request)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        Option<FillerKind> maybeFillerKind = Option<FillerKind>.None;
        if (Enum.TryParse(request.FillerKind, ignoreCase: true, out FillerKind fk))
        {
            maybeFillerKind = fk;
        }

        engine.PadToNext(
            request.Content,
            request.Minutes,
            request.Fallback,
            request.Trim,
            request.DiscardAttempts,
            request.StopBeforeEnd,
            request.OfflineTail,
            maybeFillerKind,
            request.CustomTitle,
            request.DisableWatermarks);
        return Ok();
    }

    [HttpPost("pad_until", Name = "PadUntil")]
    public IActionResult PadUntil([FromRoute] Guid buildId, [FromBody] PadUntilRequestModel request)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        Option<FillerKind> maybeFillerKind = Option<FillerKind>.None;
        if (Enum.TryParse(request.FillerKind, ignoreCase: true, out FillerKind fk))
        {
            maybeFillerKind = fk;
        }

        engine.PadUntil(
            request.Content,
            request.When,
            request.Tomorrow,
            request.Fallback,
            request.Trim,
            request.DiscardAttempts,
            request.StopBeforeEnd,
            request.OfflineTail,
            maybeFillerKind,
            request.CustomTitle,
            request.DisableWatermarks);
        return Ok();
    }

    [HttpGet("start_epg_group", Name = "StartEpgGroup")]
    public IActionResult StartEpgGroup([FromRoute] Guid buildId, [FromBody] StartEpgGroupRequestModel request)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        engine.LockGuideGroup(request.Advance);
        return Ok();
    }

    [HttpGet("stop_epg_group", Name = "StopEpgGroup")]
    public IActionResult StopEpgGroup([FromRoute] Guid buildId)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        engine.UnlockGuideGroup();
        return Ok();
    }

    [HttpGet("graphics_on", Name = "GraphicsOn")]
    public async Task<IActionResult> GraphicsOn(
        [FromRoute]
        Guid buildId,
        [FromBody]
        GraphicsOnRequestModel request,
        CancellationToken cancellationToken = default)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        await engine.GraphicsOn(request.Graphics, request.Variables, cancellationToken);
        return Ok();
    }

    [HttpGet("graphics_off", Name = "GraphicsOff")]
    public async Task<IActionResult> GraphicsOff(
        [FromRoute]
        Guid buildId,
        [FromBody]
        GraphicsOffRequestModel request,
        CancellationToken cancellationToken = default)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        await engine.GraphicsOff(request.Graphics, cancellationToken);
        return Ok();
    }

    [HttpGet("watermark_on", Name = "WatermarkOn")]
    public async Task<IActionResult> WatermarkOn([FromRoute] Guid buildId, [FromBody] WatermarkOnRequestModel request)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        await engine.WatermarkOn(request.Watermark);
        return Ok();
    }

    [HttpGet("watermark_off", Name = "WatermarkOff")]
    public async Task<IActionResult> WatermarkOff([FromRoute] Guid buildId, [FromBody] WatermarkOffRequestModel request)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        await engine.WatermarkOff(request.Watermark);
        return Ok();
    }

    [HttpGet("skip_items", Name = "SkipItems")]
    public IActionResult SkipItems([FromRoute] Guid buildId, [FromBody] SkipItemsRequestModel request)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        engine.SkipItems(request.Content, request.Count);
        return Ok();
    }

    [HttpGet("skip_to_item", Name = "SkipToItem")]
    public IActionResult SkipToItem([FromRoute] Guid buildId, [FromBody] SkipToItemRequestModel request)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        engine.SkipToItem(request.Content, request.Season, request.Episode);
        return Ok();
    }

    [HttpGet("wait_until", Name = "WaitUntil")]
    public IActionResult WaitUntil(
        [FromRoute]
        Guid buildId,
        [FromBody]
        WaitUntilRequestModel request)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        if (TimeOnly.TryParse(request.When, out TimeOnly waitUntil))
        {
            engine.WaitUntil(waitUntil, request.Tomorrow, request.RewindOnReset);
        }

        return Ok();
    }
}
