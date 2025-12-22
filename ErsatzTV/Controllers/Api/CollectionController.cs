#nullable enable
using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.MediaCards;
using ErsatzTV.Application.MediaCollections;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[EndpointGroupName("general")]
public class CollectionController(IMediator mediator) : ControllerBase
{
    // Collections
    [HttpGet("/api/collections", Name = "GetCollections")]
    [Tags("Collections")]
    [EndpointSummary("Get all collections (paginated)")]
    public async Task<PagedMediaCollectionsViewModel> GetCollections(
        [FromQuery] string query = "",
        [FromQuery] int pageNumber = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        await mediator.Send(new GetPagedCollections(query, pageNumber, pageSize), cancellationToken);

    [HttpGet("/api/collections/all", Name = "GetAllCollections")]
    [Tags("Collections")]
    [EndpointSummary("Get all collections")]
    public async Task<List<MediaCollectionViewModel>> GetAllCollections(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllCollections(), cancellationToken);

    [HttpGet("/api/collections/{id:int}", Name = "GetCollectionById")]
    [Tags("Collections")]
    [EndpointSummary("Get collection by ID")]
    public async Task<IActionResult> GetCollectionById(int id, CancellationToken cancellationToken)
    {
        Option<MediaCollectionViewModel> result = await mediator.Send(new GetCollectionById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpGet("/api/collections/{id:int}/items", Name = "GetCollectionItems")]
    [Tags("Collections")]
    [EndpointSummary("Get collection items")]
    public async Task<IActionResult> GetCollectionItems(int id, CancellationToken cancellationToken)
    {
        Either<BaseError, CollectionCardResultsViewModel> result = await mediator.Send(new GetCollectionCards(id), cancellationToken);
        return result.Match<IActionResult>(Ok, error => NotFound());
    }

    [HttpPost("/api/collections", Name = "CreateCollection")]
    [Tags("Collections")]
    [EndpointSummary("Create a new collection")]
    public async Task<IActionResult> CreateCollection(
        [Required] [FromBody] CreateCollectionRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, MediaCollectionViewModel> result = await mediator.Send(new CreateCollection(request.Name), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpPut("/api/collections/{id:int}", Name = "UpdateCollection")]
    [Tags("Collections")]
    [EndpointSummary("Update a collection")]
    public async Task<IActionResult> UpdateCollection(
        int id,
        [Required] [FromBody] UpdateCollectionRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(new UpdateCollection(id, request.Name), cancellationToken);
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }

    [HttpDelete("/api/collections/{id:int}", Name = "DeleteCollection")]
    [Tags("Collections")]
    [EndpointSummary("Delete a collection")]
    public async Task<IActionResult> DeleteCollection(int id, CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(new DeleteCollection(id), cancellationToken);
        return result.Match<IActionResult>(_ => NoContent(), error => Problem(error.ToString()));
    }

    [HttpPost("/api/collections/{id:int}/items", Name = "AddItemsToCollection")]
    [Tags("Collections")]
    [EndpointSummary("Add items to a collection")]
    public async Task<IActionResult> AddItemsToCollection(
        int id,
        [Required] [FromBody] AddItemsToCollectionRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(
            new AddItemsToCollection(
                id,
                request.MovieIds ?? [],
                request.ShowIds ?? [],
                request.SeasonIds ?? [],
                request.EpisodeIds ?? [],
                request.ArtistIds ?? [],
                request.MusicVideoIds ?? [],
                request.OtherVideoIds ?? [],
                request.SongIds ?? [],
                request.ImageIds ?? [],
                request.RemoteStreamIds ?? []),
            cancellationToken);
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }

    [HttpDelete("/api/collections/{id:int}/items", Name = "RemoveItemsFromCollection")]
    [Tags("Collections")]
    [EndpointSummary("Remove items from a collection")]
    public async Task<IActionResult> RemoveItemsFromCollection(
        int id,
        [Required] [FromBody] RemoveItemsFromCollectionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RemoveItemsFromCollection(id) { MediaItemIds = request.MediaItemIds ?? [] };
        Either<BaseError, Unit> result = await mediator.Send(command, cancellationToken);
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }

    // Multi-collections
    [HttpGet("/api/collections/multi", Name = "GetMultiCollections")]
    [Tags("Collections")]
    [EndpointSummary("Get all multi-collections")]
    public async Task<List<MultiCollectionViewModel>> GetMultiCollections(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllMultiCollections(), cancellationToken);

    [HttpGet("/api/collections/multi/{id:int}", Name = "GetMultiCollectionById")]
    [Tags("Collections")]
    [EndpointSummary("Get multi-collection by ID")]
    public async Task<IActionResult> GetMultiCollectionById(int id, CancellationToken cancellationToken)
    {
        Option<MultiCollectionViewModel> result = await mediator.Send(new GetMultiCollectionById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpPost("/api/collections/multi", Name = "CreateMultiCollection")]
    [Tags("Collections")]
    [EndpointSummary("Create a multi-collection")]
    public async Task<IActionResult> CreateMultiCollection(
        [Required] [FromBody] CreateMultiCollectionRequest request,
        CancellationToken cancellationToken)
    {
        var items = request.Items?.Select(i => new CreateMultiCollectionItem(
            i.CollectionId, i.SmartCollectionId, i.ScheduleAsGroup, i.PlaybackOrder)).ToList() ?? [];
        Either<BaseError, MultiCollectionViewModel> result = await mediator.Send(
            new CreateMultiCollection(request.Name, items), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpPut("/api/collections/multi/{id:int}", Name = "UpdateMultiCollection")]
    [Tags("Collections")]
    [EndpointSummary("Update a multi-collection")]
    public async Task<IActionResult> UpdateMultiCollection(
        int id,
        [Required] [FromBody] UpdateMultiCollectionRequest request,
        CancellationToken cancellationToken)
    {
        var items = request.Items?.Select(i => new UpdateMultiCollectionItem(
            i.CollectionId, i.SmartCollectionId, i.ScheduleAsGroup, i.PlaybackOrder)).ToList() ?? [];
        Either<BaseError, Unit> result = await mediator.Send(
            new UpdateMultiCollection(id, request.Name, items), cancellationToken);
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }

    [HttpDelete("/api/collections/multi/{id:int}", Name = "DeleteMultiCollection")]
    [Tags("Collections")]
    [EndpointSummary("Delete a multi-collection")]
    public async Task<IActionResult> DeleteMultiCollection(int id, CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(new DeleteMultiCollection(id), cancellationToken);
        return result.Match<IActionResult>(_ => NoContent(), error => Problem(error.ToString()));
    }
}

// Request models
public record CreateCollectionRequest(string Name);
public record UpdateCollectionRequest(string Name);
public record AddItemsToCollectionRequest(
    List<int>? MovieIds,
    List<int>? ShowIds,
    List<int>? SeasonIds,
    List<int>? EpisodeIds,
    List<int>? ArtistIds,
    List<int>? MusicVideoIds,
    List<int>? OtherVideoIds,
    List<int>? SongIds,
    List<int>? ImageIds,
    List<int>? RemoteStreamIds);
public record RemoveItemsFromCollectionRequest(List<int>? MediaItemIds);
public record CreateMultiCollectionItemRequest(int? CollectionId, int? SmartCollectionId, bool ScheduleAsGroup, PlaybackOrder PlaybackOrder);
public record CreateMultiCollectionRequest(string Name, List<CreateMultiCollectionItemRequest>? Items);
public record UpdateMultiCollectionItemRequest(int? CollectionId, int? SmartCollectionId, bool ScheduleAsGroup, PlaybackOrder PlaybackOrder);
public record UpdateMultiCollectionRequest(string Name, List<UpdateMultiCollectionItemRequest>? Items);
