#nullable enable
using System.ComponentModel.DataAnnotations;
using System.Threading.Channels;
using ErsatzTV.Application;
using ErsatzTV.Application.Artworks;
using ErsatzTV.Application.Channels;
using ErsatzTV.Application.Playouts;
using ErsatzTV.Core;
using ErsatzTV.Core.Api.Channels;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Scheduling;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
public class ChannelController(ChannelWriter<IBackgroundServiceRequest> workerChannel, IMediator mediator) : ControllerBase
{
    [HttpGet("/api/channels", Name = "GetAllChannels")]
    [Tags("Channels")]
    [EndpointSummary("Get all channels")]
    [EndpointGroupName("general")]
    
    public async Task<List<ChannelResponseModel>> GetAll(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllChannelsForApi(), cancellationToken);

    [HttpGet("/api/channels/{id:int}", Name = "GetChannelById")]
    [Tags("Channels")]
    [EndpointSummary("Get channel by ID")]
    [EndpointGroupName("general")]
    
    public async Task<IActionResult> GetChannelById(int id, CancellationToken cancellationToken)
    {
        Option<ChannelViewModel> result = await mediator.Send(new GetChannelById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpGet("/api/channels/by-number/{channelNumber}", Name = "GetChannelByNumber")]
    [Tags("Channels")]
    [EndpointSummary("Get channel by number")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> GetChannelByNumber(string channelNumber, CancellationToken cancellationToken)
    {
        Option<ChannelViewModel> result = await mediator.Send(new GetChannelByNumber(channelNumber), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpPost("/api/channels", Name = "CreateChannel")]
    [Tags("Channels")]
    [EndpointSummary("Create a channel")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> CreateChannel(
        [Required] [FromBody] CreateChannelRequest request,
        CancellationToken cancellationToken)
    {
        var logo = request.LogoPath != null
            ? new ArtworkContentTypeModel(request.LogoPath, request.LogoContentType ?? "")
            : ArtworkContentTypeModel.None;

        Either<BaseError, CreateChannelResult> result = await mediator.Send(
            new CreateChannel(
                request.Name,
                request.Number,
                request.Group ?? "",
                request.Categories ?? "",
                request.FFmpegProfileId,
                logo,
                request.StreamSelectorMode,
                request.StreamSelector ?? "",
                request.PreferredAudioLanguageCode ?? "",
                request.PreferredAudioTitle ?? "",
                request.PlayoutSource,
                request.PlayoutMode,
                request.MirrorSourceChannelId,
                request.PlayoutOffset,
                request.StreamingMode,
                request.WatermarkId,
                request.FallbackFillerId,
                request.PreferredSubtitleLanguageCode ?? "",
                request.SubtitleMode,
                request.MusicVideoCreditsMode,
                request.MusicVideoCreditsTemplate ?? "",
                request.SongVideoMode,
                request.TranscodeMode,
                request.IdleBehavior,
                request.IsEnabled,
                request.ShowInEpg),
            cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpPut("/api/channels/{id:int}", Name = "UpdateChannel")]
    [Tags("Channels")]
    [EndpointSummary("Update a channel")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> UpdateChannel(
        int id,
        [Required] [FromBody] UpdateChannelRequest request,
        CancellationToken cancellationToken)
    {
        var logo = request.LogoPath != null
            ? new ArtworkContentTypeModel(request.LogoPath, request.LogoContentType ?? "")
            : ArtworkContentTypeModel.None;

        Either<BaseError, ChannelViewModel> result = await mediator.Send(
            new UpdateChannel(
                id,
                request.Name,
                request.Number,
                request.Group ?? "",
                request.Categories ?? "",
                request.FFmpegProfileId,
                logo,
                request.StreamSelectorMode,
                request.StreamSelector ?? "",
                request.PreferredAudioLanguageCode ?? "",
                request.PreferredAudioTitle ?? "",
                request.PlayoutSource,
                request.PlayoutMode,
                request.MirrorSourceChannelId,
                request.PlayoutOffset,
                request.StreamingMode,
                request.WatermarkId,
                request.FallbackFillerId,
                request.PreferredSubtitleLanguageCode ?? "",
                request.SubtitleMode,
                request.MusicVideoCreditsMode,
                request.MusicVideoCreditsTemplate ?? "",
                request.SongVideoMode,
                request.TranscodeMode,
                request.IdleBehavior,
                request.IsEnabled,
                request.ShowInEpg),
            cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpDelete("/api/channels/{id:int}", Name = "DeleteChannel")]
    [Tags("Channels")]
    [EndpointSummary("Delete a channel")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> DeleteChannel(int id, CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(new DeleteChannel(id), cancellationToken);
        return result.Match<IActionResult>(_ => NoContent(), error => Problem(error.ToString()));
    }

    [HttpPost("/api/channels/{channelNumber}/playout/reset", Name = "ResetChannelPlayout")]
    [Tags("Channels")]
    [EndpointSummary("Reset channel playout")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> ResetPlayout(string channelNumber)
    {
        Option<int> maybePlayoutId = await mediator.Send(new GetPlayoutIdByChannelNumber(channelNumber));
        foreach (int playoutId in maybePlayoutId)
        {
            await workerChannel.WriteAsync(new BuildPlayout(playoutId, PlayoutBuildMode.Reset));
            return Ok();
        }

        return NotFound();
    }

    [HttpPost("/api/channels/{channelNumber}/playout/build", Name = "BuildChannelPlayout")]
    [Tags("Channels")]
    [EndpointSummary("Build channel playout")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> BuildPlayout(string channelNumber)
    {
        Option<int> maybePlayoutId = await mediator.Send(new GetPlayoutIdByChannelNumber(channelNumber));
        foreach (int playoutId in maybePlayoutId)
        {
            await workerChannel.WriteAsync(new BuildPlayout(playoutId, PlayoutBuildMode.Refresh));
            return Accepted();
        }

        return NotFound();
    }
}

// Request models
public record CreateChannelRequest(
    string Name,
    string Number,
    string? Group,
    string? Categories,
    int FFmpegProfileId,
    string? LogoPath,
    string? LogoContentType,
    ChannelStreamSelectorMode StreamSelectorMode,
    string? StreamSelector,
    string? PreferredAudioLanguageCode,
    string? PreferredAudioTitle,
    ChannelPlayoutSource PlayoutSource,
    ChannelPlayoutMode PlayoutMode,
    int? MirrorSourceChannelId,
    TimeSpan? PlayoutOffset,
    StreamingMode StreamingMode,
    int? WatermarkId,
    int? FallbackFillerId,
    string? PreferredSubtitleLanguageCode,
    ChannelSubtitleMode SubtitleMode,
    ChannelMusicVideoCreditsMode MusicVideoCreditsMode,
    string? MusicVideoCreditsTemplate,
    ChannelSongVideoMode SongVideoMode,
    ChannelTranscodeMode TranscodeMode,
    ChannelIdleBehavior IdleBehavior,
    bool IsEnabled,
    bool ShowInEpg);

public record UpdateChannelRequest(
    string Name,
    string Number,
    string? Group,
    string? Categories,
    int FFmpegProfileId,
    string? LogoPath,
    string? LogoContentType,
    ChannelStreamSelectorMode StreamSelectorMode,
    string? StreamSelector,
    string? PreferredAudioLanguageCode,
    string? PreferredAudioTitle,
    ChannelPlayoutSource PlayoutSource,
    ChannelPlayoutMode PlayoutMode,
    int? MirrorSourceChannelId,
    TimeSpan? PlayoutOffset,
    StreamingMode StreamingMode,
    int? WatermarkId,
    int? FallbackFillerId,
    string? PreferredSubtitleLanguageCode,
    ChannelSubtitleMode SubtitleMode,
    ChannelMusicVideoCreditsMode MusicVideoCreditsMode,
    string? MusicVideoCreditsTemplate,
    ChannelSongVideoMode SongVideoMode,
    ChannelTranscodeMode TranscodeMode,
    ChannelIdleBehavior IdleBehavior,
    bool IsEnabled,
    bool ShowInEpg);
