#nullable enable
using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Application.Tree;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[EndpointGroupName("general")]
public class PlaylistController(IMediator mediator) : ControllerBase
{
    // Playlist Groups
    [HttpGet("/api/playlists/groups", Name = "GetPlaylistGroups")]
    [Tags("Playlists")]
    [EndpointSummary("Get all playlist groups")]
    public async Task<List<PlaylistGroupViewModel>> GetPlaylistGroups(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllPlaylistGroups(), cancellationToken);

    [HttpPost("/api/playlists/groups", Name = "CreatePlaylistGroup")]
    [Tags("Playlists")]
    [EndpointSummary("Create a playlist group")]
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
    public async Task<IActionResult> DeletePlaylistGroup(int id, CancellationToken cancellationToken)
    {
        Option<BaseError> result = await mediator.Send(new DeletePlaylistGroup(id), cancellationToken);
        return result.Match<IActionResult>(error => Problem(error.ToString()), () => NoContent());
    }

    // Playlists
    [HttpGet("/api/playlists/groups/{groupId:int}/playlists", Name = "GetPlaylistsByGroup")]
    [Tags("Playlists")]
    [EndpointSummary("Get playlists by group")]
    public async Task<List<PlaylistViewModel>> GetPlaylistsByGroup(int groupId, CancellationToken cancellationToken) =>
        await mediator.Send(new GetPlaylistsByPlaylistGroupId(groupId), cancellationToken);

    [HttpGet("/api/playlists/tree", Name = "GetPlaylistTree")]
    [Tags("Playlists")]
    [EndpointSummary("Get playlist tree")]
    public async Task<TreeViewModel> GetPlaylistTree(CancellationToken cancellationToken) =>
        await mediator.Send(new GetPlaylistTree(), cancellationToken);

    [HttpGet("/api/playlists/{id:int}", Name = "GetPlaylistById")]
    [Tags("Playlists")]
    [EndpointSummary("Get playlist by ID")]
    public async Task<IActionResult> GetPlaylistById(int id, CancellationToken cancellationToken)
    {
        Option<PlaylistViewModel> result = await mediator.Send(new GetPlaylistById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpGet("/api/playlists/{id:int}/items", Name = "GetPlaylistItems")]
    [Tags("Playlists")]
    [EndpointSummary("Get playlist items")]
    public async Task<List<PlaylistItemViewModel>> GetPlaylistItems(int id, CancellationToken cancellationToken) =>
        await mediator.Send(new GetPlaylistItems(id), cancellationToken);

    [HttpPost("/api/playlists", Name = "CreatePlaylist")]
    [Tags("Playlists")]
    [EndpointSummary("Create a playlist")]
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
    public async Task<IActionResult> DeletePlaylist(int id, CancellationToken cancellationToken)
    {
        Option<BaseError> result = await mediator.Send(new DeletePlaylist(id), cancellationToken);
        return result.Match<IActionResult>(error => Problem(error.ToString()), () => NoContent());
    }

    [HttpPut("/api/playlists/{id:int}/items", Name = "ReplacePlaylistItems")]
    [Tags("Playlists")]
    [EndpointSummary("Replace playlist items")]
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
