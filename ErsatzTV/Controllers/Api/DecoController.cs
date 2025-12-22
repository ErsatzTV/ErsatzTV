#nullable enable
using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.Scheduling;
using ErsatzTV.Application.Tree;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Scheduling;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[EndpointGroupName("general")]
public class DecoController(IMediator mediator) : ControllerBase
{
    // Deco Groups
    [HttpGet("/api/decos/groups", Name = "GetDecoGroups")]
    [Tags("Decos")]
    [EndpointSummary("Get all deco groups")]
    public async Task<List<DecoGroupViewModel>> GetDecoGroups(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllDecoGroups(), cancellationToken);

    [HttpPost("/api/decos/groups", Name = "CreateDecoGroup")]
    [Tags("Decos")]
    [EndpointSummary("Create a deco group")]
    public async Task<IActionResult> CreateDecoGroup(
        [Required] [FromBody] CreateDecoGroupRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, DecoGroupViewModel> result = await mediator.Send(
            new CreateDecoGroup(request.Name), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpDelete("/api/decos/groups/{id:int}", Name = "DeleteDecoGroup")]
    [Tags("Decos")]
    [EndpointSummary("Delete a deco group")]
    public async Task<IActionResult> DeleteDecoGroup(int id, CancellationToken cancellationToken)
    {
        Option<BaseError> result = await mediator.Send(new DeleteDecoGroup(id), cancellationToken);
        return result.Match<IActionResult>(error => Problem(error.ToString()), () => NoContent());
    }

    // Decos
    [HttpGet("/api/decos/groups/{groupId:int}/decos", Name = "GetDecosByGroup")]
    [Tags("Decos")]
    [EndpointSummary("Get decos by group")]
    public async Task<List<DecoViewModel>> GetDecosByGroup(int groupId, CancellationToken cancellationToken) =>
        await mediator.Send(new GetDecosByDecoGroupId(groupId), cancellationToken);

    [HttpGet("/api/decos/tree", Name = "GetDecoTree")]
    [Tags("Decos")]
    [EndpointSummary("Get deco tree")]
    public async Task<TreeViewModel> GetDecoTree(CancellationToken cancellationToken) =>
        await mediator.Send(new GetDecoTree(), cancellationToken);

    [HttpGet("/api/decos/{id:int}", Name = "GetDecoById")]
    [Tags("Decos")]
    [EndpointSummary("Get deco by ID")]
    public async Task<IActionResult> GetDecoById(int id, CancellationToken cancellationToken)
    {
        Option<DecoViewModel> result = await mediator.Send(new GetDecoById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpPost("/api/decos", Name = "CreateDeco")]
    [Tags("Decos")]
    [EndpointSummary("Create a deco")]
    public async Task<IActionResult> CreateDeco(
        [Required] [FromBody] CreateDecoRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, DecoViewModel> result = await mediator.Send(
            new CreateDeco(request.DecoGroupId, request.Name), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpPut("/api/decos/{id:int}", Name = "UpdateDeco")]
    [Tags("Decos")]
    [EndpointSummary("Update a deco")]
    public async Task<IActionResult> UpdateDeco(
        int id,
        [Required] [FromBody] UpdateDecoRequest request,
        CancellationToken cancellationToken)
    {
        var breakContent = request.BreakContent?.Select(b => new UpdateDecoBreakContent(
            b.Id,
            b.CollectionType,
            b.CollectionId,
            b.MediaItemId,
            b.MultiCollectionId,
            b.SmartCollectionId,
            b.PlaylistId,
            b.Placement)).ToList() ?? [];

        Either<BaseError, Unit> result = await mediator.Send(
            new UpdateDeco(
                id,
                request.DecoGroupId,
                request.Name,
                request.WatermarkMode,
                request.WatermarkIds ?? [],
                request.UseWatermarkDuringFiller,
                request.GraphicsElementsMode,
                request.GraphicsElementIds ?? [],
                request.UseGraphicsElementsDuringFiller,
                request.BreakContentMode,
                breakContent,
                request.DefaultFillerMode,
                request.DefaultFillerCollectionType,
                request.DefaultFillerCollectionId,
                request.DefaultFillerMediaItemId,
                request.DefaultFillerMultiCollectionId,
                request.DefaultFillerSmartCollectionId,
                request.DefaultFillerTrimToFit,
                request.DeadAirFallbackMode,
                request.DeadAirFallbackCollectionType,
                request.DeadAirFallbackCollectionId,
                request.DeadAirFallbackMediaItemId,
                request.DeadAirFallbackMultiCollectionId,
                request.DeadAirFallbackSmartCollectionId),
            cancellationToken);
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }

    [HttpDelete("/api/decos/{id:int}", Name = "DeleteDeco")]
    [Tags("Decos")]
    [EndpointSummary("Delete a deco")]
    public async Task<IActionResult> DeleteDeco(int id, CancellationToken cancellationToken)
    {
        Option<BaseError> result = await mediator.Send(new DeleteDeco(id), cancellationToken);
        return result.Match<IActionResult>(error => Problem(error.ToString()), () => NoContent());
    }

    [HttpPut("/api/playouts/{playoutId:int}/deco", Name = "UpdateDefaultDeco")]
    [Tags("Decos")]
    [EndpointSummary("Update default deco for playout")]
    public async Task<IActionResult> UpdateDefaultDeco(
        int playoutId,
        [Required] [FromBody] UpdateDefaultDecoRequest request,
        CancellationToken cancellationToken)
    {
        Option<BaseError> result = await mediator.Send(
            new UpdateDefaultDeco(playoutId, request.DecoId), cancellationToken);
        return result.Match<IActionResult>(error => Problem(error.ToString()), () => Ok());
    }

    // Deco Template Groups
    [HttpGet("/api/decos/template-groups", Name = "GetDecoTemplateGroups")]
    [Tags("Decos")]
    [EndpointSummary("Get all deco template groups")]
    public async Task<List<DecoTemplateGroupViewModel>> GetDecoTemplateGroups(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllDecoTemplateGroups(), cancellationToken);

    [HttpPost("/api/decos/template-groups", Name = "CreateDecoTemplateGroup")]
    [Tags("Decos")]
    [EndpointSummary("Create a deco template group")]
    public async Task<IActionResult> CreateDecoTemplateGroup(
        [Required] [FromBody] CreateDecoTemplateGroupRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, DecoTemplateGroupViewModel> result = await mediator.Send(
            new CreateDecoTemplateGroup(request.Name), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpDelete("/api/decos/template-groups/{id:int}", Name = "DeleteDecoTemplateGroup")]
    [Tags("Decos")]
    [EndpointSummary("Delete a deco template group")]
    public async Task<IActionResult> DeleteDecoTemplateGroup(int id, CancellationToken cancellationToken)
    {
        Option<BaseError> result = await mediator.Send(new DeleteDecoTemplateGroup(id), cancellationToken);
        return result.Match<IActionResult>(error => Problem(error.ToString()), () => NoContent());
    }

    // Deco Templates
    [HttpGet("/api/decos/template-groups/{groupId:int}/templates", Name = "GetDecoTemplatesByGroup")]
    [Tags("Decos")]
    [EndpointSummary("Get deco templates by group")]
    public async Task<List<DecoTemplateViewModel>> GetDecoTemplatesByGroup(int groupId, CancellationToken cancellationToken) =>
        await mediator.Send(new GetDecoTemplatesByDecoTemplateGroupId(groupId), cancellationToken);

    [HttpGet("/api/decos/templates/tree", Name = "GetDecoTemplateTree")]
    [Tags("Decos")]
    [EndpointSummary("Get deco template tree")]
    public async Task<TreeViewModel> GetDecoTemplateTree(CancellationToken cancellationToken) =>
        await mediator.Send(new GetDecoTemplateTree(), cancellationToken);

    [HttpGet("/api/decos/templates/{id:int}", Name = "GetDecoTemplateById")]
    [Tags("Decos")]
    [EndpointSummary("Get deco template by ID")]
    public async Task<IActionResult> GetDecoTemplateById(int id, CancellationToken cancellationToken)
    {
        Option<DecoTemplateViewModel> result = await mediator.Send(new GetDecoTemplateById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpGet("/api/decos/templates/{id:int}/items", Name = "GetDecoTemplateItems")]
    [Tags("Decos")]
    [EndpointSummary("Get deco template items")]
    public async Task<List<DecoTemplateItemViewModel>> GetDecoTemplateItems(int id, CancellationToken cancellationToken) =>
        await mediator.Send(new GetDecoTemplateItems(id), cancellationToken);

    [HttpPost("/api/decos/templates", Name = "CreateDecoTemplate")]
    [Tags("Decos")]
    [EndpointSummary("Create a deco template")]
    public async Task<IActionResult> CreateDecoTemplate(
        [Required] [FromBody] CreateDecoTemplateRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, DecoTemplateViewModel> result = await mediator.Send(
            new CreateDecoTemplate(request.DecoTemplateGroupId, request.Name), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpDelete("/api/decos/templates/{id:int}", Name = "DeleteDecoTemplate")]
    [Tags("Decos")]
    [EndpointSummary("Delete a deco template")]
    public async Task<IActionResult> DeleteDecoTemplate(int id, CancellationToken cancellationToken)
    {
        Option<BaseError> result = await mediator.Send(new DeleteDecoTemplate(id), cancellationToken);
        return result.Match<IActionResult>(error => Problem(error.ToString()), () => NoContent());
    }
}

// Request models
public record CreateDecoGroupRequest(string Name);
public record CreateDecoRequest(int DecoGroupId, string Name);
public record UpdateDecoBreakContentRequest(
    int Id,
    CollectionType CollectionType,
    int? CollectionId,
    int? MediaItemId,
    int? MultiCollectionId,
    int? SmartCollectionId,
    int? PlaylistId,
    DecoBreakPlacement Placement);
public record UpdateDecoRequest(
    int DecoGroupId,
    string Name,
    DecoMode WatermarkMode,
    List<int>? WatermarkIds,
    bool UseWatermarkDuringFiller,
    DecoMode GraphicsElementsMode,
    List<int>? GraphicsElementIds,
    bool UseGraphicsElementsDuringFiller,
    DecoMode BreakContentMode,
    List<UpdateDecoBreakContentRequest>? BreakContent,
    DecoMode DefaultFillerMode,
    CollectionType DefaultFillerCollectionType,
    int? DefaultFillerCollectionId,
    int? DefaultFillerMediaItemId,
    int? DefaultFillerMultiCollectionId,
    int? DefaultFillerSmartCollectionId,
    bool DefaultFillerTrimToFit,
    DecoMode DeadAirFallbackMode,
    CollectionType DeadAirFallbackCollectionType,
    int? DeadAirFallbackCollectionId,
    int? DeadAirFallbackMediaItemId,
    int? DeadAirFallbackMultiCollectionId,
    int? DeadAirFallbackSmartCollectionId);
public record UpdateDefaultDecoRequest(int? DecoId);
public record CreateDecoTemplateGroupRequest(string Name);
public record CreateDecoTemplateRequest(int DecoTemplateGroupId, string Name);
