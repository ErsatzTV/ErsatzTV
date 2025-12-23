#nullable enable
using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.Filler;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Filler;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]

public class FillerController(IMediator mediator) : ControllerBase
{
    [HttpGet("/api/filler/presets", Name = "GetFillerPresets")]
    [Tags("Filler")]
    [EndpointSummary("Get all filler presets (paginated)")]
    [EndpointGroupName("general")]
    public async Task<PagedFillerPresetsViewModel> GetFillerPresets(
        [FromQuery] int pageNumber = 0,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        await mediator.Send(new GetPagedFillerPresets(pageNumber, pageSize), cancellationToken);

    [HttpGet("/api/filler/presets/all", Name = "GetAllFillerPresets")]
    [Tags("Filler")]
    [EndpointSummary("Get all filler presets")]
    [EndpointGroupName("general")]
    public async Task<List<FillerPresetViewModel>> GetAllFillerPresets(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllFillerPresets(), cancellationToken);

    [HttpGet("/api/filler/presets/{id:int}", Name = "GetFillerPresetById")]
    [Tags("Filler")]
    [EndpointSummary("Get filler preset by ID")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> GetFillerPresetById(int id, CancellationToken cancellationToken)
    {
        Option<FillerPresetViewModel> result = await mediator.Send(new GetFillerPresetById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpPost("/api/filler/presets", Name = "CreateFillerPreset")]
    [Tags("Filler")]
    [EndpointSummary("Create a filler preset")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> CreateFillerPreset(
        [Required] [FromBody] CreateFillerPresetRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(
            new CreateFillerPreset(
                request.Name,
                request.FillerKind,
                request.FillerMode,
                request.Duration,
                request.Count,
                request.PadToNearestMinute,
                request.AllowWatermarks,
                request.CollectionType,
                request.CollectionId,
                request.MediaItemId,
                request.MultiCollectionId,
                request.SmartCollectionId,
                request.PlaylistId,
                request.Expression ?? "",
                request.UseChaptersAsMediaItems),
            cancellationToken);
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }

    [HttpPut("/api/filler/presets/{id:int}", Name = "UpdateFillerPreset")]
    [Tags("Filler")]
    [EndpointSummary("Update a filler preset")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> UpdateFillerPreset(
        int id,
        [Required] [FromBody] UpdateFillerPresetRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(
            new UpdateFillerPreset(
                id,
                request.Name,
                request.FillerKind,
                request.FillerMode,
                request.Duration,
                request.Count,
                request.PadToNearestMinute,
                request.AllowWatermarks,
                request.CollectionType,
                request.CollectionId,
                request.MediaItemId,
                request.MultiCollectionId,
                request.SmartCollectionId,
                request.PlaylistId,
                request.Expression ?? "",
                request.UseChaptersAsMediaItems),
            cancellationToken);
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }

    [HttpDelete("/api/filler/presets/{id:int}", Name = "DeleteFillerPreset")]
    [Tags("Filler")]
    [EndpointSummary("Delete a filler preset")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> DeleteFillerPreset(int id, CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(new DeleteFillerPreset(id), cancellationToken);
        return result.Match<IActionResult>(_ => NoContent(), error => Problem(error.ToString()));
    }
}

// Request models
public record CreateFillerPresetRequest(
    string Name,
    FillerKind FillerKind,
    FillerMode FillerMode,
    TimeSpan? Duration,
    int? Count,
    int? PadToNearestMinute,
    bool AllowWatermarks,
    CollectionType CollectionType,
    int? CollectionId,
    int? MediaItemId,
    int? MultiCollectionId,
    int? SmartCollectionId,
    int? PlaylistId,
    string? Expression,
    bool UseChaptersAsMediaItems);

public record UpdateFillerPresetRequest(
    string Name,
    FillerKind FillerKind,
    FillerMode FillerMode,
    TimeSpan? Duration,
    int? Count,
    int? PadToNearestMinute,
    bool AllowWatermarks,
    CollectionType CollectionType,
    int? CollectionId,
    int? MediaItemId,
    int? MultiCollectionId,
    int? SmartCollectionId,
    int? PlaylistId,
    string? Expression,
    bool UseChaptersAsMediaItems);
