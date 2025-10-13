using ErsatzTV.Application.Resolutions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[EndpointGroupName("general")]
public class ResolutionController(IMediator mediator) : ControllerBase
{
    [HttpGet("/api/ffmpeg/resolution/by-name/{name}", Name="GetResolutionByName")]
    public async Task<ActionResult<ResolutionViewModel>> GetResolutionByName(string name, CancellationToken cancellationToken)
    {
        Option<ResolutionViewModel> result = await mediator.Send(new GetResolutionByName(name), cancellationToken);
        return result.Match<ActionResult<ResolutionViewModel>>(i => Ok(i), () => NotFound());
    }
}

