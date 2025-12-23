using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.Resolutions;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]

public class ResolutionController(IMediator mediator) : ControllerBase
{
    [HttpGet("/api/resolutions", Name = "GetAllResolutions")]
    [Tags("Resolutions")]
    [EndpointSummary("Get all resolutions")]
    [EndpointGroupName("general")]
    public async Task<List<ResolutionViewModel>> GetAllResolutions(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllResolutions(), cancellationToken);

    [HttpGet("/api/resolutions/by-name/{name}", Name = "GetResolutionByName")]
    [Tags("Resolutions")]
    [EndpointSummary("Get resolution by name")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> GetResolutionByName(string name, CancellationToken cancellationToken)
    {
        Option<ResolutionViewModel> result = await mediator.Send(new GetResolutionByName(name), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpPost("/api/resolutions", Name = "CreateCustomResolution")]
    [Tags("Resolutions")]
    [EndpointSummary("Create a custom resolution")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> CreateCustomResolution(
        [Required] [FromBody] CreateCustomResolutionRequest request,
        CancellationToken cancellationToken)
    {
        Option<Core.BaseError> result = await mediator.Send(
            new CreateCustomResolution(request.Width, request.Height), cancellationToken);
        return result.Match<IActionResult>(error => Problem(error.ToString()), () => Ok());
    }

    [HttpDelete("/api/resolutions/{id:int}", Name = "DeleteCustomResolution")]
    [Tags("Resolutions")]
    [EndpointSummary("Delete a custom resolution")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> DeleteCustomResolution(int id, CancellationToken cancellationToken)
    {
        Option<Core.BaseError> result = await mediator.Send(new DeleteCustomResolution(id), cancellationToken);
        return result.Match<IActionResult>(error => Problem(error.ToString()), () => NoContent());
    }
}

// Request models
public record CreateCustomResolutionRequest(int Width, int Height);
