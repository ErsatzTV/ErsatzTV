#nullable enable
using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.Scheduling;
using ErsatzTV.Core;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]

public class TemplateController(IMediator mediator) : ControllerBase
{
    // Template Groups
    [HttpGet("/api/templates/groups", Name = "GetTemplateGroups")]
    [Tags("Templates")]
    [EndpointSummary("Get all template groups")]
    [EndpointGroupName("general")]
    public async Task<List<TemplateGroupViewModel>> GetTemplateGroups(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllTemplateGroups(), cancellationToken);

    [HttpPost("/api/templates/groups", Name = "CreateTemplateGroup")]
    [Tags("Templates")]
    [EndpointSummary("Create a template group")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> CreateTemplateGroup(
        [Required] [FromBody] CreateTemplateGroupRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, TemplateGroupViewModel> result = await mediator.Send(
            new CreateTemplateGroup(request.Name), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpDelete("/api/templates/groups/{id:int}", Name = "DeleteTemplateGroup")]
    [Tags("Templates")]
    [EndpointSummary("Delete a template group")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> DeleteTemplateGroup(int id, CancellationToken cancellationToken)
    {
        Option<BaseError> result = await mediator.Send(new DeleteTemplateGroup(id), cancellationToken);
        return result.Match<IActionResult>(error => Problem(error.ToString()), () => NoContent());
    }

    // Templates
    [HttpGet("/api/templates/groups/{groupId:int}/templates", Name = "GetTemplatesByGroup")]
    [Tags("Templates")]
    [EndpointSummary("Get templates by group")]
    [EndpointGroupName("general")]
    public async Task<List<TemplateViewModel>> GetTemplatesByGroup(int groupId, CancellationToken cancellationToken) =>
        await mediator.Send(new GetTemplatesByTemplateGroupId(groupId), cancellationToken);

    [HttpGet("/api/templates", Name = "GetAllTemplates")]
    [Tags("Templates")]
    [EndpointSummary("Get all templates")]
    [EndpointGroupName("general")]
    public async Task<List<TemplateViewModel>> GetAllTemplates(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllTemplates(), cancellationToken);

    [HttpGet("/api/templates/{id:int}", Name = "GetTemplateById")]
    [Tags("Templates")]
    [EndpointSummary("Get template by ID")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> GetTemplateById(int id, CancellationToken cancellationToken)
    {
        Option<TemplateViewModel> result = await mediator.Send(new GetTemplateById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpGet("/api/templates/{id:int}/items", Name = "GetTemplateItems")]
    [Tags("Templates")]
    [EndpointSummary("Get template items")]
    [EndpointGroupName("general")]
    public async Task<List<TemplateItemViewModel>> GetTemplateItems(int id, CancellationToken cancellationToken) =>
        await mediator.Send(new GetTemplateItems(id), cancellationToken);

    [HttpPost("/api/templates", Name = "CreateTemplate")]
    [Tags("Templates")]
    [EndpointSummary("Create a template")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> CreateTemplate(
        [Required] [FromBody] CreateTemplateRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, TemplateViewModel> result = await mediator.Send(
            new CreateTemplate(request.TemplateGroupId, request.Name), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpDelete("/api/templates/{id:int}", Name = "DeleteTemplate")]
    [Tags("Templates")]
    [EndpointSummary("Delete a template")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> DeleteTemplate(int id, CancellationToken cancellationToken)
    {
        Option<BaseError> result = await mediator.Send(new DeleteTemplate(id), cancellationToken);
        return result.Match<IActionResult>(error => Problem(error.ToString()), () => NoContent());
    }

    [HttpPost("/api/templates/{id:int}/copy", Name = "CopyTemplate")]
    [Tags("Templates")]
    [EndpointSummary("Copy a template")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> CopyTemplate(
        int id,
        [Required] [FromBody] CopyTemplateRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, TemplateViewModel> result = await mediator.Send(
            new CopyTemplate(id, request.NewTemplateGroupId, request.NewTemplateName), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpPut("/api/templates/{id:int}/items", Name = "ReplaceTemplateItems")]
    [Tags("Templates")]
    [EndpointSummary("Replace template items")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> ReplaceTemplateItems(
        int id,
        [Required] [FromBody] ReplaceTemplateItemsRequest request,
        CancellationToken cancellationToken)
    {
        var items = request.Items.Select(i => new ReplaceTemplateItem(i.BlockId, i.StartTime)).ToList();
        Either<BaseError, List<TemplateItemViewModel>> result = await mediator.Send(
            new ReplaceTemplateItems(request.TemplateGroupId, id, request.Name, items), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }
}

// Request models
public record CreateTemplateGroupRequest(string Name);
public record CreateTemplateRequest(int TemplateGroupId, string Name);
public record CopyTemplateRequest(int NewTemplateGroupId, string NewTemplateName);
public record ReplaceTemplateItemRequest(int BlockId, TimeSpan StartTime);
public record ReplaceTemplateItemsRequest(int TemplateGroupId, string Name, List<ReplaceTemplateItemRequest> Items);
