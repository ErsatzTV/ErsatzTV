using System.ComponentModel.DataAnnotations;
using ErsatzTV.Core;
using ErsatzTV.Application.FFmpegProfiles;
using ErsatzTV.Core.Api.FFmpegProfiles;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
public class FFmpegProfileController
{
    private readonly IMediator _mediator;

    public FFmpegProfileController(IMediator mediator) => _mediator = mediator;

    [HttpGet("/api/ffmpeg/profiles")]
    public async Task<List<FFmpegProfileResponseModel>> GetAll() =>
        await _mediator.Send(new GetAllFFmpegProfilesForApi());

    [HttpPost("/api/ffmpeg/profiles/new")]
    public async Task<Either<BaseError, CreateFFmpegProfileResult>> AddOne(
        [Required] [FromBody]
        CreateFFmpegProfile request) => await _mediator.Send(request);

    [HttpPut("/api/ffmpeg/profiles/update")]
    public async Task<Either<BaseError, UpdateFFmpegProfileResult>> UpdateOne(
        [Required] [FromBody]
        UpdateFFmpegProfile request) => await _mediator.Send(request);

    [HttpGet("/api/ffmpeg/profiles/{id}")]
    public async Task<Option<FFmpegFullProfileResponseModel>> GetOne(int id) =>
        await _mediator.Send(new GetFFmpegFullProfileByIdForApi(id));

    [HttpDelete("/api/ffmpeg/delete/{id}")]
    public async Task DeleteProfileAsync(int id) => await _mediator.Send(new DeleteFFmpegProfile(id));
}
