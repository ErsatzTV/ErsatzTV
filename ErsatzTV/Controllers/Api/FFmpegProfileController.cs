using System.ComponentModel.DataAnnotations;
using ErsatzTV.Application.FFmpegProfiles;
using ErsatzTV.Core;
using ErsatzTV.Core.Api.FFmpegProfiles;
using ErsatzTV.Filters;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
[V2ApiActionFilter]
public class FFmpegProfileController(IMediator mediator)
{
    [HttpGet("/api/ffmpeg/profiles")]
    public async Task<List<FFmpegProfileResponseModel>> GetAll() =>
        await mediator.Send(new GetAllFFmpegProfilesForApi());

    [HttpPost("/api/ffmpeg/profiles/new")]
    public async Task<Either<BaseError, CreateFFmpegProfileResult>> AddOne(
        [Required] [FromBody]
        CreateFFmpegProfile request) => await mediator.Send(request);

    [HttpPut("/api/ffmpeg/profiles/update")]
    public async Task<Either<BaseError, UpdateFFmpegProfileResult>> UpdateOne(
        [Required] [FromBody]
        UpdateFFmpegProfile request) => await mediator.Send(request);

    [HttpGet("/api/ffmpeg/profiles/{id:int}")]
    public async Task<Option<FFmpegFullProfileResponseModel>> GetOne(int id) =>
        await mediator.Send(new GetFFmpegFullProfileByIdForApi(id));

    [HttpDelete("/api/ffmpeg/delete/{id:int}")]
    public async Task DeleteProfileAsync(int id) => await mediator.Send(new DeleteFFmpegProfile(id));
}
