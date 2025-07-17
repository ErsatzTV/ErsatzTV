using System.Threading.Channels;
using CliWrap;
using ErsatzTV.Application;
using ErsatzTV.Application.Troubleshooting;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Metadata;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
public class TroubleshootController(
    IEntityLocker entityLocker,
    ChannelWriter<IFFmpegWorkerRequest> channelWriter,
    ILocalFileSystem localFileSystem,
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
                string playlistFile = Path.Combine(FileSystemLayout.TranscodeFolder, ".troubleshooting", "live.m3u8");

                DateTimeOffset start =  DateTimeOffset.Now;
                while (!localFileSystem.FileExists(playlistFile) &&
                       DateTimeOffset.Now - start < TimeSpan.FromSeconds(15))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }

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
