using ErsatzTV.Application.FFmpegProfiles;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[EndpointGroupName("general")]
public class ResolutionController(IMediator mediator) : ControllerBase
{
    [HttpGet("/api/ffmpeg/resolution/by-name/{name}", Name="GetResolutionByName")]
    public async Task<ActionResult<int>> GetResolutionByName(string name, CancellationToken cancellationToken)
    {
        Option<int> result = await mediator.Send(new GetResolutionByName(name), cancellationToken);
        return result.Match<ActionResult<int>>(i => Ok(i), () => NotFound());
    }
}

