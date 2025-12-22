#nullable enable
using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.Scheduling;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[EndpointGroupName("general")]
public class BlockController(IMediator mediator) : ControllerBase
{
    // Block Groups
    [HttpGet("/api/blocks/groups", Name = "GetBlockGroups")]
    [Tags("Blocks")]
    [EndpointSummary("Get all block groups")]
    public async Task<List<BlockGroupViewModel>> GetBlockGroups(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllBlockGroups(), cancellationToken);

    [HttpPost("/api/blocks/groups", Name = "CreateBlockGroup")]
    [Tags("Blocks")]
    [EndpointSummary("Create a block group")]
    public async Task<IActionResult> CreateBlockGroup(
        [Required] [FromBody] CreateBlockGroupRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, BlockGroupViewModel> result = await mediator.Send(
            new CreateBlockGroup(request.Name), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpDelete("/api/blocks/groups/{id:int}", Name = "DeleteBlockGroup")]
    [Tags("Blocks")]
    [EndpointSummary("Delete a block group")]
    public async Task<IActionResult> DeleteBlockGroup(int id, CancellationToken cancellationToken)
    {
        Option<BaseError> result = await mediator.Send(new DeleteBlockGroup(id), cancellationToken);
        return result.Match<IActionResult>(error => Problem(error.ToString()), () => NoContent());
    }

    // Blocks
    [HttpGet("/api/blocks/groups/{groupId:int}/blocks", Name = "GetBlocksByGroup")]
    [Tags("Blocks")]
    [EndpointSummary("Get blocks by group")]
    public async Task<List<BlockViewModel>> GetBlocksByGroup(int groupId, CancellationToken cancellationToken) =>
        await mediator.Send(new GetBlocksByBlockGroupId(groupId), cancellationToken);

    [HttpGet("/api/blocks", Name = "GetAllBlocks")]
    [Tags("Blocks")]
    [EndpointSummary("Get all blocks")]
    public async Task<List<BlockViewModel>> GetAllBlocks(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllBlocks(), cancellationToken);

    [HttpGet("/api/blocks/{id:int}", Name = "GetBlockById")]
    [Tags("Blocks")]
    [EndpointSummary("Get block by ID")]
    public async Task<IActionResult> GetBlockById(int id, CancellationToken cancellationToken)
    {
        Option<BlockViewModel> result = await mediator.Send(new GetBlockById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpGet("/api/blocks/{id:int}/items", Name = "GetBlockItems")]
    [Tags("Blocks")]
    [EndpointSummary("Get block items")]
    public async Task<List<BlockItemViewModel>> GetBlockItems(int id, CancellationToken cancellationToken) =>
        await mediator.Send(new GetBlockItems(id), cancellationToken);

    [HttpPost("/api/blocks", Name = "CreateBlock")]
    [Tags("Blocks")]
    [EndpointSummary("Create a block")]
    public async Task<IActionResult> CreateBlock(
        [Required] [FromBody] CreateBlockRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, BlockViewModel> result = await mediator.Send(
            new CreateBlock(request.BlockGroupId, request.Name), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpDelete("/api/blocks/{id:int}", Name = "DeleteBlock")]
    [Tags("Blocks")]
    [EndpointSummary("Delete a block")]
    public async Task<IActionResult> DeleteBlock(int id, CancellationToken cancellationToken)
    {
        Option<BaseError> result = await mediator.Send(new DeleteBlock(id), cancellationToken);
        return result.Match<IActionResult>(error => Problem(error.ToString()), () => NoContent());
    }

    [HttpPost("/api/blocks/{id:int}/copy", Name = "CopyBlock")]
    [Tags("Blocks")]
    [EndpointSummary("Copy a block")]
    public async Task<IActionResult> CopyBlock(
        int id,
        [Required] [FromBody] CopyBlockRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, BlockViewModel> result = await mediator.Send(
            new CopyBlock(id, request.NewBlockGroupId, request.NewBlockName), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpPut("/api/blocks/{id:int}/items", Name = "ReplaceBlockItems")]
    [Tags("Blocks")]
    [EndpointSummary("Replace block items")]
    public async Task<IActionResult> ReplaceBlockItems(
        int id,
        [Required] [FromBody] ReplaceBlockItemsRequest request,
        CancellationToken cancellationToken)
    {
        var items = request.Items?.Select(i => new ReplaceBlockItem(
            i.Index,
            i.CollectionType,
            i.CollectionId,
            i.MultiCollectionId,
            i.SmartCollectionId,
            i.MediaItemId,
            i.SearchTitle ?? "",
            i.SearchQuery ?? "",
            i.PlaybackOrder,
            i.IncludeInProgramGuide,
            i.DisableWatermarks,
            i.WatermarkIds ?? [],
            i.GraphicsElementIds ?? [])).ToList() ?? [];
        Either<BaseError, Unit> result = await mediator.Send(
            new ReplaceBlockItems(request.BlockGroupId, id, request.Name, request.Minutes, request.StopScheduling, items),
            cancellationToken);
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }
}

// Request models
public record CreateBlockGroupRequest(string Name);
public record CreateBlockRequest(int BlockGroupId, string Name);
public record CopyBlockRequest(int NewBlockGroupId, string NewBlockName);
public record ReplaceBlockItemRequest(
    int Index,
    CollectionType CollectionType,
    int? CollectionId,
    int? MultiCollectionId,
    int? SmartCollectionId,
    int? MediaItemId,
    string? SearchTitle,
    string? SearchQuery,
    PlaybackOrder PlaybackOrder,
    bool IncludeInProgramGuide,
    bool DisableWatermarks,
    List<int>? WatermarkIds,
    List<int>? GraphicsElementIds);
public record ReplaceBlockItemsRequest(
    int BlockGroupId,
    string Name,
    int Minutes,
    BlockStopScheduling StopScheduling,
    List<ReplaceBlockItemRequest>? Items);
