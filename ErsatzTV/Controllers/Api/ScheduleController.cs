#nullable enable
using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.Filler;
using ErsatzTV.Application.Graphics;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Application.ProgramSchedules;
using ErsatzTV.Application.Watermarks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
public class ScheduleController(IMediator mediator) : ControllerBase
{
    private static CollectionApiResponse? ToApiResponse(MediaCollectionViewModel? c) =>
        c == null ? null : new(c.Id, c.Name, c.CollectionType, c.UseCustomPlaybackOrder, c.State);

    private static SmartCollectionApiResponse? ToApiResponse(SmartCollectionViewModel? s) =>
        s == null ? null : new(s.Id, s.Name, s.Query);

    private static MultiCollectionApiResponse? ToApiResponse(MultiCollectionViewModel? mc) =>
        mc == null ? null : new(
            mc.Id,
            mc.Name,
            mc.Items?.Select(i => new MultiCollectionItemApiResponse(
                i.MultiCollectionId,
                i.Collection?.Id ?? 0,
                i.Collection?.Name ?? "",
                i.Collection?.CollectionType ?? CollectionType.Collection,
                i.Collection?.UseCustomPlaybackOrder ?? false,
                i.Collection?.State ?? MediaItemState.Normal,
                i.ScheduleAsGroup,
                i.PlaybackOrder)).ToList() ?? [],
            mc.SmartItems?.Select(s => new MultiCollectionSmartItemApiResponse(
                s.MultiCollectionId,
                s.SmartCollection?.Id ?? 0,
                s.SmartCollection?.Name ?? "",
                s.SmartCollection?.Query ?? "",
                s.ScheduleAsGroup,
                s.PlaybackOrder)).ToList() ?? []);

    private static MediaItemApiResponse? ToMediaItemApiResponse(NamedMediaItemViewModel? m) =>
        m == null ? null : new(m.MediaItemId, m.Name);

    private static RerunCollectionApiResponse? ToApiResponse(RerunCollectionViewModel? r) =>
        r == null ? null : new(r.Id, r.Name);

    private static PlaylistApiResponse? ToApiResponse(PlaylistViewModel? p) =>
        p == null ? null : new(p.Id, p.PlaylistGroupId, p.Name, p.IsSystem);

    private static FillerPresetApiResponse? ToApiResponse(FillerPresetViewModel? f) =>
        f == null ? null : new(f.Id, f.Name);

    private static WatermarkApiResponse ToApiResponse(WatermarkViewModel w) =>
        new(w.Id, w.Name, w.Image?.Path, w.Mode, w.ImageSource, w.Location, w.Size, w.Width,
            w.HorizontalMargin, w.VerticalMargin, w.FrequencyMinutes, w.DurationSeconds,
            w.Opacity, w.PlaceWithinSourceContent, w.OpacityExpression, w.ZIndex);

    private static GraphicsElementApiResponse ToApiResponse(GraphicsElementViewModel g) =>
        new(g.Id, g.Name, g.FileName);

    private static ScheduleItemApiResponse ToApiResponse(ProgramScheduleItemViewModel item) =>
        new(
            item.Id,
            item.Index,
            item.StartType,
            item.StartTime,
            item.FixedStartTimeBehavior,
            item.PlayoutMode,
            item.CollectionType,
            ToApiResponse(item.Collection),
            ToApiResponse(item.MultiCollection),
            ToApiResponse(item.SmartCollection),
            ToApiResponse(item.RerunCollection),
            ToApiResponse(item.Playlist),
            ToMediaItemApiResponse(item.MediaItem),
            item.SearchTitle,
            item.SearchQuery,
            item.PlaybackOrder,
            item.MarathonGroupBy,
            item.MarathonShuffleGroups,
            item.MarathonShuffleItems,
            item.MarathonBatchSize,
            item.FillWithGroupMode,
            item.CustomTitle,
            item.GuideMode,
            ToApiResponse(item.PreRollFiller),
            ToApiResponse(item.MidRollFiller),
            ToApiResponse(item.PostRollFiller),
            ToApiResponse(item.TailFiller),
            ToApiResponse(item.FallbackFiller),
            item.Watermarks?.Select(ToApiResponse).ToList() ?? [],
            item.GraphicsElements?.Select(ToApiResponse).ToList() ?? [],
            item.PreferredAudioLanguageCode,
            item.PreferredAudioTitle,
            item.PreferredSubtitleLanguageCode,
            item.SubtitleMode,
            item is ProgramScheduleItemMultipleViewModel multiple ? multiple.Count : null,
            item is ProgramScheduleItemDurationViewModel duration ? duration.PlayoutDuration : null);
    [HttpGet("/api/schedules", Name = "GetAllSchedules")]
    [Tags("Schedules")]
    [EndpointSummary("Get all schedules (paginated)")]
    [EndpointGroupName("general")]
    public async Task<PagedProgramSchedulesViewModel> GetAllSchedules(
        [FromQuery] string query = "",
        [FromQuery] int pageNumber = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        await mediator.Send(new GetPagedProgramSchedules(query, pageNumber, pageSize), cancellationToken);

    [HttpGet("/api/schedules/all", Name = "GetAllSchedulesList")]
    [Tags("Schedules")]
    [EndpointSummary("Get all schedules")]
    [EndpointGroupName("general")]
    public async Task<List<ProgramScheduleViewModel>> GetAllSchedulesList(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllProgramSchedules(), cancellationToken);

    [HttpGet("/api/schedules/{id:int}", Name = "GetScheduleById")]
    [Tags("Schedules")]
    [EndpointSummary("Get schedule by ID")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> GetScheduleById(int id, CancellationToken cancellationToken)
    {
        Option<ProgramScheduleViewModel> result = await mediator.Send(new GetProgramScheduleById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpGet("/api/schedules/{id:int}/items", Name = "GetScheduleItems")]
    [Tags("Schedules")]
    [EndpointSummary("Get schedule items")]
    [EndpointGroupName("general")]
    public async Task<List<ScheduleItemApiResponse>> GetScheduleItems(int id, CancellationToken cancellationToken)
    {
        List<ProgramScheduleItemViewModel> items = await mediator.Send(new GetProgramScheduleItems(id), cancellationToken);
        return items.Select(ToApiResponse).ToList();
    }

    [HttpPost("/api/schedules", Name = "CreateSchedule")]
    [Tags("Schedules")]
    [EndpointSummary("Create a schedule")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> CreateSchedule(
        [Required] [FromBody] CreateScheduleRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, CreateProgramScheduleResult> result = await mediator.Send(
            new CreateProgramSchedule(
                request.Name,
                request.KeepMultiPartEpisodesTogether,
                request.TreatCollectionsAsShows,
                request.ShuffleScheduleItems,
                request.RandomStartPoint,
                request.FixedStartTimeBehavior),
            cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpDelete("/api/schedules/{id:int}", Name = "DeleteSchedule")]
    [Tags("Schedules")]
    [EndpointSummary("Delete a schedule")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> DeleteSchedule(int id, CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(new DeleteProgramSchedule(id), cancellationToken);
        return result.Match<IActionResult>(_ => NoContent(), error => Problem(error.ToString()));
    }

    [HttpPost("/api/schedules/{id:int}/copy", Name = "CopySchedule")]
    [Tags("Schedules")]
    [EndpointSummary("Copy a schedule")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> CopySchedule(
        int id,
        [Required] [FromBody] CopyScheduleRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, ProgramScheduleViewModel> result = await mediator.Send(
            new CopyProgramSchedule(id, request.Name), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }
}

// Request models
public record CreateScheduleRequest(
    string Name,
    bool KeepMultiPartEpisodesTogether,
    bool TreatCollectionsAsShows,
    bool ShuffleScheduleItems,
    bool RandomStartPoint,
    FixedStartTimeBehavior FixedStartTimeBehavior);

public record CopyScheduleRequest(string Name);
