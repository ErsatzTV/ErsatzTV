using ErsatzTV.Core.Api.ScriptedPlayout;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using ErsatzTV.Core.Interfaces.Scheduling;
using ErsatzTV.Core.Scheduling.Engine;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[EndpointGroupName("scripted-schedule")]
[Route("api/scripted/playout/build/{buildId:guid}")]
public class ScriptedScheduleController(IScriptedPlayoutBuilderService scriptedPlayoutBuilderService) : ControllerBase
{
    [HttpGet("context", Name = "GetContext")]
    [Tags("Scripted Metadata")]
    [EndpointSummary("Get the current context")]
    public ActionResult<ContextResponseModel> GetContext([FromRoute] Guid buildId)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        return GetContextInternal(engine);
    }

    [HttpPost("add_collection", Name = "AddCollection")]
    [Tags("Scripted Content")]
    [EndpointSummary("Add a collection")]
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
    [Tags("Scripted Content")]
    [EndpointSummary("Add a marathon")]
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
    [Tags("Scripted Content")]
    [EndpointSummary("Add a multi-collection")]
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
    [Tags("Scripted Content")]
    [EndpointSummary("Add a playlist")]
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

    [HttpPost("add_search", Name = "AddSearch")]
    [Tags("Scripted Content")]
    [EndpointSummary("Add a search query")]
    public async Task<IActionResult> AddSearch(
        [FromRoute]
        Guid buildId,
        [FromBody]
        AddSearchQueryRequestModel request,
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

        await engine.AddSearch(request.Key, request.Query, playbackOrder, cancellationToken);
        return Ok();
    }

    [HttpPost("add_smart_collection", Name = "AddSmartCollection")]
    [Tags("Scripted Content")]
    [EndpointSummary("Add a smart collection")]
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
    [Tags("Scripted Content")]
    [EndpointSummary("Add a show")]
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
    [Tags("Scripted Scheduling")]
    [EndpointSummary("Add all content")]
    public ActionResult<ContextResponseModel> AddAll([FromRoute] Guid buildId, [FromBody] AddAllRequestModel request)
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
        return GetContextInternal(engine);
    }

    [HttpPost("add_count", Name = "AddCount")]
    [Tags("Scripted Scheduling")]
    [EndpointSummary("Add a specific number of content items")]
    public ActionResult<ContextResponseModel> AddCount(
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
        return GetContextInternal(engine);
    }

    [HttpPost("add_duration", Name = "AddDuration")]
    [Tags("Scripted Scheduling")]
    [EndpointSummary("Add content for a specific duration")]
    public ActionResult<ContextResponseModel> AddDuration(
        [FromRoute]
        Guid buildId,
        [FromBody]
        AddDurationRequestModel request)
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
        return GetContextInternal(engine);
    }

    [HttpPost("pad_to_next", Name = "PadToNext")]
    [Tags("Scripted Scheduling")]
    [EndpointSummary("Add content until a specific minutes interval")]
    public ActionResult<ContextResponseModel> PadToNext(
        [FromRoute] Guid buildId,
        [FromBody] PadToNextRequestModel request)
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
        return GetContextInternal(engine);
    }

    [HttpPost("pad_until", Name = "PadUntil")]
    [Tags("Scripted Scheduling")]
    [EndpointSummary("Add content until a specified time of day")]
    public ActionResult<ContextResponseModel> PadUntil(
        [FromRoute] Guid buildId,
        [FromBody] PadUntilRequestModel request)
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
        return GetContextInternal(engine);
    }

    [HttpPost("pad_until_exact", Name = "PadUntilExact")]
    [Tags("Scripted Scheduling")]
    [EndpointSummary("Add content until an exact time")]
    public ActionResult<ContextResponseModel> PadUntilExact(
        [FromRoute]
        Guid buildId,
        [FromBody]
        PadUntilExactRequestModel request)
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

        engine.PadUntilExact(
            request.Content,
            request.When,
            request.Fallback,
            request.Trim,
            request.DiscardAttempts,
            request.StopBeforeEnd,
            request.OfflineTail,
            maybeFillerKind,
            request.CustomTitle,
            request.DisableWatermarks);
        return GetContextInternal(engine);
    }

    [HttpGet("peek_next/{content}", Name="PeekNext")]
    [Tags("Scripted Scheduling")]
    [EndpointSummary("Peek the next content item")]
    public ActionResult<PeekItemResponseModel> PeekNext(Guid buildId, string content)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        Option<MediaItem> maybeMediaItem = engine.PeekNext(content);
        foreach (var mediaItem in maybeMediaItem)
        {
            return new PeekItemResponseModel
            {
                Content = content,
                Milliseconds = (long)engine.DurationForMediaItem(mediaItem).TotalMilliseconds
            };
        }

        return NotFound("Content key does not exist, or collection is empty");
    }

    [HttpPost("start_epg_group", Name = "StartEpgGroup")]
    [Tags("Scripted Control")]
    [EndpointSummary("Start an EPG group")]
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

    [HttpPost("stop_epg_group", Name = "StopEpgGroup")]
    [Tags("Scripted Control")]
    [EndpointSummary("Finish an EPG group")]
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

    [HttpPost("graphics_on", Name = "GraphicsOn")]
    [Tags("Scripted Control")]
    [EndpointSummary("Turn on graphics elements")]
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

    [HttpPost("graphics_off", Name = "GraphicsOff")]
    [Tags("Scripted Control")]
    [EndpointSummary("Turn off graphics elements")]
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

    [HttpPost("watermark_on", Name = "WatermarkOn")]
    [Tags("Scripted Control")]
    [EndpointSummary("Turn on watermarks")]
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

    [HttpPost("watermark_off", Name = "WatermarkOff")]
    [Tags("Scripted Control")]
    [EndpointSummary("Turn off watermarks")]
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

    [HttpPost("skip_items", Name = "SkipItems")]
    [Tags("Scripted Control")]
    [EndpointSummary("Skip a specific number of items")]
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

    [HttpPost("skip_to_item", Name = "SkipToItem")]
    [Tags("Scripted Control")]
    [EndpointSummary("Skip to a specific episode")]
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

    [HttpPost("wait_until_exact", Name = "WaitUntilExact")]
    [Tags("Scripted Control")]
    [EndpointSummary("Wait until an exact time")]
    public ActionResult<ContextResponseModel> WaitUntilExact(
        [FromRoute]
        Guid buildId,
        [FromBody]
        WaitUntilExactRequestModel request)
    {
        ISchedulingEngine engine = scriptedPlayoutBuilderService.GetEngine(buildId);
        if (engine == null)
        {
            return NotFound($"Active build engine not found for build {buildId}.");
        }

        engine.WaitUntilExact(request.When, request.RewindOnReset);

        return GetContextInternal(engine);
    }

    [HttpPost("wait_until", Name = "WaitUntil")]
    [Tags("Scripted Control")]
    [EndpointSummary("Wait until the specified time of day")]
    public ActionResult<ContextResponseModel> WaitUntilTime(
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

        return GetContextInternal(engine);
    }

    private ActionResult<ContextResponseModel> GetContextInternal(ISchedulingEngine engine)
    {
        try
        {
            var state = engine.GetState();
            var responseModel = new ContextResponseModel
            {
                CurrentTime = state.CurrentTime,
                StartTime = state.Start,
                FinishTime = state.Finish,
                IsDone = state.IsDone
            };
            return Ok(responseModel);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
