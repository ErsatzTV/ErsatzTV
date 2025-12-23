using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]

public class SessionController(IFFmpegSegmenterService ffmpegSegmenterService)
{
    [HttpGet("api/sessions")]
    [Tags("Sessions")]
    [EndpointSummary("Get sessions")]
    [EndpointGroupName("general")]
    public List<HlsSessionModel> GetSessions() => ffmpegSegmenterService.Workers.Map(w => w.GetModel()).ToList();

    [HttpDelete("api/session/{channelNumber}")]
    [Tags("Sessions")]
    [EndpointSummary("Stop session")]
    [EndpointGroupName("general")]
    public async Task<IActionResult> StopSession(string channelNumber, CancellationToken cancellationToken)
    {
        if (await ffmpegSegmenterService.StopChannel(channelNumber, cancellationToken))
        {
            return new NoContentResult();
        }

        return new NotFoundResult();
    }
}
