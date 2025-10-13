using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.FFmpegProfiles;
using ErsatzTV.Core;
using ErsatzTV.Core.Api.FFmpegProfiles;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[EndpointGroupName("general")]
public class FFmpegProfileController(IMediator mediator) : ControllerBase
{
    [HttpGet("/api/ffmpeg/profiles", Name="GetFFmpegProfiles")]
    public async Task<List<FFmpegFullProfileResponseModel>> GetAll(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllFFmpegProfilesForApi(), cancellationToken);

    [HttpPost("/api/ffmpeg/profiles/new", Name="CreateFFmpegProfile")]
    public async Task<IActionResult> AddOne(
        [Required] [FromBody]
        CreateFFmpegProfile request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, CreateFFmpegProfileResult> result =  await mediator.Send(request, cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpPut("/api/ffmpeg/profiles/update", Name="UpdateFFmpegProfile")]
    public async Task<IActionResult> UpdateOne(
        [Required] [FromBody]
        UpdateFFmpegProfile request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, UpdateFFmpegProfileResult> result = await mediator.Send(request, cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpDelete("/api/ffmpeg/delete/{id:int}", Name="DeleteFFmpegProfile")]
    public async Task<IActionResult> DeleteProfileAsync(int id, CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(new DeleteFFmpegProfile(id), cancellationToken);
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }
}
