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
}
