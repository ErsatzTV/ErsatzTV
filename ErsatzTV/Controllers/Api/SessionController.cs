using ErsatzTV.Core.FFmpeg;
using ErsatzTV.Core.Interfaces.FFmpeg;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
public class SessionController(IFFmpegSegmenterService ffmpegSegmenterService)
{
    [HttpGet("api/sessions")]
    public List<HlsSessionModel> GetSessions()
    {
        return ffmpegSegmenterService.SessionWorkers.Values.Map(w => w.GetModel()).ToList();
    }

    [HttpDelete("api/session/{channelNumber}")]
    public async Task<IActionResult> StopSession(string channelNumber, CancellationToken cancellationToken)
    {
        if (ffmpegSegmenterService.SessionWorkers.TryGetValue(channelNumber, out IHlsSessionWorker worker))
        {
            await worker.Cancel(cancellationToken);
            return new NoContentResult();
        }

        return new NotFoundResult();
    }
}
