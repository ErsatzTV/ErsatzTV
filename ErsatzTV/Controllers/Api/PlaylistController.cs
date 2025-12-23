#nullable enable
using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Application.Tree;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
public class PlaylistController(IMediator mediator) : ControllerBase
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

    private static PlaylistItemApiResponse ToApiResponse(PlaylistItemViewModel item) =>
        new(
            item.Id,
            item.Index,
            item.CollectionType,
            ToApiResponse(item.Collection),
            ToApiResponse(item.MultiCollection),
            ToApiResponse(item.SmartCollection),
            ToMediaItemApiResponse(item.MediaItem),
            item.PlaybackOrder,
            item.Count,
            item.PlayAll,
            item.IncludeInProgramGuide);
    // Playlist Groups
    [HttpGet("/api/playlists/groups", Name = "GetPlaylistGroups")]
    [Tags("Playlists")]
    [EndpointSummary("Get all playlist groups")]
    [EndpointGroupName("general")]
    public async Task<List<PlaylistGroupViewModel>> GetPlaylistGroups(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllPlaylistGroups(), cancellationToken);

    [HttpPost("/api/playlists/groups", Name = "CreatePlaylistGroup")]
    [Tags("Playlists")]
    [EndpointSummary("Create a playlist group")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> CreatePlaylistGroup(
        [Required] [FromBody] CreatePlaylistGroupRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, PlaylistGroupViewModel> result = await mediator.Send(
            new CreatePlaylistGroup(request.Name), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpDelete("/api/playlists/groups/{id:int}", Name = "DeletePlaylistGroup")]
    [Tags("Playlists")]
    [EndpointSummary("Delete a playlist group")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> DeletePlaylistGroup(int id, CancellationToken cancellationToken)
    {
        Option<BaseError> result = await mediator.Send(new DeletePlaylistGroup(id), cancellationToken);
        return result.Match<IActionResult>(error => Problem(error.ToString()), () => NoContent());
    }

    // Playlists
    [HttpGet("/api/playlists/groups/{groupId:int}/playlists", Name = "GetPlaylistsByGroup")]
    [Tags("Playlists")]
    [EndpointSummary("Get playlists by group")]
    [EndpointGroupName("general")]
    public async Task<List<PlaylistViewModel>> GetPlaylistsByGroup(int groupId, CancellationToken cancellationToken) =>
        await mediator.Send(new GetPlaylistsByPlaylistGroupId(groupId), cancellationToken);

    [HttpGet("/api/playlists/tree", Name = "GetPlaylistTree")]
    [Tags("Playlists")]
    [EndpointSummary("Get playlist tree")]
    [EndpointGroupName("general")]
    public async Task<TreeViewModel> GetPlaylistTree(CancellationToken cancellationToken) =>
        await mediator.Send(new GetPlaylistTree(), cancellationToken);

    [HttpGet("/api/playlists/{id:int}", Name = "GetPlaylistById")]
    [Tags("Playlists")]
    [EndpointSummary("Get playlist by ID")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> GetPlaylistById(int id, CancellationToken cancellationToken)
    {
        Option<PlaylistViewModel> result = await mediator.Send(new GetPlaylistById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpGet("/api/playlists/{id:int}/items", Name = "GetPlaylistItems")]
    [Tags("Playlists")]
    [EndpointSummary("Get playlist items")]
    [EndpointGroupName("general")]
    public async Task<List<PlaylistItemApiResponse>> GetPlaylistItems(int id, CancellationToken cancellationToken)
    {
        List<PlaylistItemViewModel> items = await mediator.Send(new GetPlaylistItems(id), cancellationToken);
        return items.Select(ToApiResponse).ToList();
    }

    [HttpPost("/api/playlists", Name = "CreatePlaylistApi")]
    [Tags("Playlists")]
    [EndpointSummary("Create a playlist")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> CreatePlaylist(
        [Required] [FromBody] CreatePlaylistRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, PlaylistViewModel> result = await mediator.Send(
            new CreatePlaylist(request.PlaylistGroupId, request.Name), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpDelete("/api/playlists/{id:int}", Name = "DeletePlaylist")]
    [Tags("Playlists")]
    [EndpointSummary("Delete a playlist")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> DeletePlaylist(int id, CancellationToken cancellationToken)
    {
        Option<BaseError> result = await mediator.Send(new DeletePlaylist(id), cancellationToken);
        return result.Match<IActionResult>(error => Problem(error.ToString()), () => NoContent());
    }

    [HttpPut("/api/playlists/{id:int}/items", Name = "ReplacePlaylistItems")]
    [Tags("Playlists")]
    [EndpointSummary("Replace playlist items")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> ReplacePlaylistItems(
        int id,
        [Required] [FromBody] ReplacePlaylistItemsRequest request,
        CancellationToken cancellationToken)
    {
        var items = request.Items?.Select(i => new ReplacePlaylistItem(
            i.Index,
            i.CollectionType,
            i.CollectionId,
            i.MultiCollectionId,
            i.SmartCollectionId,
            i.MediaItemId,
            i.PlaybackOrder,
            i.Count,
            i.PlayAll,
            i.IncludeInProgramGuide)).ToList() ?? [];
        Either<BaseError, List<PlaylistItemViewModel>> result = await mediator.Send(
            new ReplacePlaylistItems(id, request.Name, items), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }
}

// Request models
public record CreatePlaylistGroupRequest(string Name);
public record CreatePlaylistRequest(int PlaylistGroupId, string Name);
public record ReplacePlaylistItemRequest(
    int Index,
    CollectionType CollectionType,
    int? CollectionId,
    int? MultiCollectionId,
    int? SmartCollectionId,
    int? MediaItemId,
    PlaybackOrder PlaybackOrder,
    int? Count,
    bool PlayAll,
    bool IncludeInProgramGuide);
public record ReplacePlaylistItemsRequest(string Name, List<ReplacePlaylistItemRequest>? Items);
