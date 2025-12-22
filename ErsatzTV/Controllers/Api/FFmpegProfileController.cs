using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.FFmpegProfiles;
using ErsatzTV.Core;
using ErsatzTV.Core.Api.FFmpegProfiles;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[EndpointGroupName("general")]
public class FFmpegProfileController(IMediator mediator) : ControllerBase
{
    [HttpGet("/api/ffmpeg/profiles", Name = "GetFFmpegProfiles")]
    [Tags("FFmpeg")]
    [EndpointSummary("Get all FFmpeg profiles")]
    public async Task<List<FFmpegFullProfileResponseModel>> GetAll(CancellationToken cancellationToken) =>
        await mediator.Send(new GetAllFFmpegProfilesForApi(), cancellationToken);

    [HttpGet("/api/ffmpeg/profiles/{id:int}", Name = "GetFFmpegProfileById")]
    [Tags("FFmpeg")]
    [EndpointSummary("Get FFmpeg profile by ID")]
    public async Task<IActionResult> GetFFmpegProfileById(int id, CancellationToken cancellationToken)
    {
        Option<FFmpegProfileViewModel> result = await mediator.Send(new GetFFmpegProfileById(id), cancellationToken);
        return result.Match<IActionResult>(Ok, () => NotFound());
    }

    [HttpPost("/api/ffmpeg/profiles/new", Name = "CreateFFmpegProfile")]
    [Tags("FFmpeg")]
    [EndpointSummary("Create FFmpeg profile")]
    public async Task<IActionResult> AddOne(
        [Required] [FromBody]
        CreateFFmpegProfile request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, CreateFFmpegProfileResult> result = await mediator.Send(request, cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpPut("/api/ffmpeg/profiles/update", Name = "UpdateFFmpegProfile")]
    [Tags("FFmpeg")]
    [EndpointSummary("Update FFmpeg profile")]
    public async Task<IActionResult> UpdateOne(
        [Required] [FromBody]
        UpdateFFmpegProfile request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, UpdateFFmpegProfileResult> result = await mediator.Send(request, cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpDelete("/api/ffmpeg/delete/{id:int}", Name = "DeleteFFmpegProfile")]
    [Tags("FFmpeg")]
    [EndpointSummary("Delete FFmpeg profile")]
    public async Task<IActionResult> DeleteProfileAsync(int id, CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(new DeleteFFmpegProfile(id), cancellationToken);
        return result.Match<IActionResult>(_ => NoContent(), error => Conflict(error.ToString()));
    }

    [HttpPost("/api/ffmpeg/profiles/{id:int}/copy", Name = "CopyFFmpegProfile")]
    [Tags("FFmpeg")]
    [EndpointSummary("Copy FFmpeg profile")]
    public async Task<IActionResult> CopyFFmpegProfile(
        int id,
        [Required] [FromBody] CopyFFmpegProfileRequest request,
        CancellationToken cancellationToken)
    {
        Either<BaseError, FFmpegProfileViewModel> result = await mediator.Send(
            new CopyFFmpegProfile(id, request.Name), cancellationToken);
        return result.Match<IActionResult>(Ok, error => Problem(error.ToString()));
    }

    [HttpGet("/api/ffmpeg/settings", Name = "GetFFmpegSettings")]
    [Tags("FFmpeg")]
    [EndpointSummary("Get FFmpeg settings")]
    public async Task<FFmpegSettingsViewModel> GetFFmpegSettings(CancellationToken cancellationToken) =>
        await mediator.Send(new GetFFmpegSettings(), cancellationToken);

    [HttpPut("/api/ffmpeg/settings", Name = "UpdateFFmpegSettings")]
    [Tags("FFmpeg")]
    [EndpointSummary("Update FFmpeg settings")]
    public async Task<IActionResult> UpdateFFmpegSettings(
        [Required] [FromBody] FFmpegSettingsViewModel settings,
        CancellationToken cancellationToken)
    {
        Either<BaseError, Unit> result = await mediator.Send(new UpdateFFmpegSettings(settings), cancellationToken);
        return result.Match<IActionResult>(_ => Ok(), error => Problem(error.ToString()));
    }
}

// Request models
public record CopyFFmpegProfileRequest(string Name);
