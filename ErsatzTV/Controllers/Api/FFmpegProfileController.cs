using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Application.FFmpegProfiles;
using ErsatzTV.Core.Api.FFmpegProfiles;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

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
        JObject data
        )
    {
        return await _mediator.Send(
            new CreateFFmpegProfileForApi(
                data.GetValue("Name").ToString(),
                (int)data.GetValue("ThreadCount"),
                (HardwareAccelerationKind)(int)data.GetValue("HardwareAcceleration"),
                (VaapiDriver)(int)data.GetValue("VaapiDriver"),
                data.GetValue("VaapiDevice").ToString(),
                (int)data.GetValue("ResolutionId"),
                (FFmpegProfileVideoFormat)(int)data.GetValue("VideoFormat"),
                (int)data.GetValue("VideoBitrate"),
                (int)data.GetValue("VideoBufferSize"),
                (FFmpegProfileAudioFormat)(int)data.GetValue("AudioFormat"),
                (int)data.GetValue("AudioBitrate"),
                (int)data.GetValue("AudioBufferSize"),
                (bool)data.GetValue("NormalizeLoudness"),
                (int)data.GetValue("AudioChannels"),
                (int)data.GetValue("AudioSampleRate"),
                (bool)data.GetValue("NormalizeFramerate"),
                (bool)data.GetValue("DeinterlaceVideo")
                )
            );
    }

    [HttpPut("/api/ffmpeg/profiles/update")]
    public async Task<Either<BaseError, UpdateFFmpegProfileResult>> UpdateOne(
    JObject data
    )
    {
        return await _mediator.Send(
            new UpdateFFmpegProfile(
                (int)data.GetValue("Id"),
                data.GetValue("Name").ToString(),
                (int)data.GetValue("ThreadCount"),
                (HardwareAccelerationKind)(int)data.GetValue("HardwareAcceleration"),
                (VaapiDriver)(int)data.GetValue("VaapiDriver"),
                data.GetValue("VaapiDevice").ToString(),
                (int)data.GetValue("ResolutionId"),
                (FFmpegProfileVideoFormat)(int)data.GetValue("VideoFormat"),
                (int)data.GetValue("VideoBitrate"),
                (int)data.GetValue("VideoBufferSize"),
                (FFmpegProfileAudioFormat)(int)data.GetValue("AudioFormat"),
                (int)data.GetValue("AudioBitrate"),
                (int)data.GetValue("AudioBufferSize"),
                (bool)data.GetValue("NormalizeLoudness"),
                (int)data.GetValue("AudioChannels"),
                (int)data.GetValue("AudioSampleRate"),
                (bool)data.GetValue("NormalizeFramerate"),
                (bool)data.GetValue("DeinterlaceVideo")
                )
            );
    }

    [HttpGet("/api/ffmpeg/profiles/{id}")]
    public async Task<Option<FFmpegFullProfileResponseModel>> GetOne(int id) =>
        await _mediator.Send(new GetFFmpegFullProfileByIdForApi(id));

    [HttpDelete("/api/ffmpeg/delete/{id}")]
    public async Task DeleteProfileAsync(int id)
    {
        await _mediator.Send(new DeleteFFmpegProfile(id));
    }
}
