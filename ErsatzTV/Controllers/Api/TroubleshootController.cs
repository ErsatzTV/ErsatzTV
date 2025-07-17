using System.Threading.Channels;
using CliWrap;
using ErsatzTV.Application;
using ErsatzTV.Application.Troubleshooting;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Locking;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
public class TroubleshootController(
    IEntityLocker entityLocker,
    ChannelWriter<IFFmpegWorkerRequest> channelWriter,
    IMediator mediator) : ControllerBase
{
    [HttpHead("api/troubleshoot/playback.m3u8")]
    [HttpGet("api/troubleshoot/playback.m3u8")]
    public async Task<IActionResult> TroubleshootPlayback(
        [FromQuery]
        int mediaItem,
        [FromQuery]
        int ffmpegProfile,
        [FromQuery]
        int watermark,
        CancellationToken cancellationToken)
    {
        entityLocker.LockTroubleshootingPlayback();

        Either<BaseError, Command> result = await mediator.Send(
            new PrepareTroubleshootingPlayback(mediaItem, ffmpegProfile, watermark),
            cancellationToken);

        return await result.MatchAsync<IActionResult>(
            async command =>
            {
                await channelWriter.WriteAsync(new StartTroubleshootingPlayback(command), CancellationToken.None);
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

                return Redirect("~/iptv/session/.troubleshooting/live.m3u8");
            },
            _ => NotFound());
    }

    [HttpHead("api/troubleshoot/playback/archive")]
    [HttpGet("api/troubleshoot/playback/archive")]
    public async Task<IActionResult> TroubleshootPlaybackArchive(
        [FromQuery]
        int mediaItem,
        [FromQuery]
        int ffmpegProfile,
        [FromQuery]
        int watermark,
        CancellationToken cancellationToken)
    {
        Option<string> maybeArchivePath = await mediator.Send(
            new ArchiveTroubleshootingResults(mediaItem, ffmpegProfile, watermark),
            cancellationToken);

        foreach (string archivePath in maybeArchivePath)
        {
            FileStream fs = System.IO.File.OpenRead(archivePath);
            return File(
                fs,
                "application/zip",
                $"ersatztv-troubleshooting-{DateTimeOffset.Now.ToUnixTimeSeconds()}.zip");
        }

        return NotFound();
    }

}
