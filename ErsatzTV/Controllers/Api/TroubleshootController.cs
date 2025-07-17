using System.Threading.Channels;
using CliWrap;
using ErsatzTV.Application;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Application.Troubleshooting;
using ErsatzTV.Application.Troubleshooting.Queries;
using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Metadata;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api;

[ApiController]
public class TroubleshootController(
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
        Either<BaseError, Command> result = await mediator.Send(
            new PrepareTroubleshootingPlayback(mediaItem, ffmpegProfile, watermark),
            cancellationToken);

        return await result.MatchAsync<IActionResult>(
            async command =>
            {
                Either<BaseError, MediaItemInfo> maybeMediaInfo = await mediator.Send(new GetMediaItemInfo(mediaItem), cancellationToken);
                foreach (MediaItemInfo mediaInfo in maybeMediaInfo.RightToSeq())
                {
                    TroubleshootingInfo troubleshootingInfo = await mediator.Send(
                        new GetTroubleshootingInfo(),
                        cancellationToken);

                    // filter ffmpeg profiles
                    troubleshootingInfo.FFmpegProfiles.RemoveAll(p => p.Id != ffmpegProfile);

                    // filter watermarks
                    troubleshootingInfo.Watermarks.RemoveAll(p => p.Id != watermark);

                    await channelWriter.WriteAsync(
                        new StartTroubleshootingPlayback(command, mediaInfo, troubleshootingInfo),
                        cancellationToken);

                    string playlistFile = Path.Combine(
                        FileSystemLayout.TranscodeFolder,
                        ".troubleshooting",
                        "live.m3u8");

                    while (!localFileSystem.FileExists(playlistFile))
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                    }

                    return Redirect("~/iptv/session/.troubleshooting/live.m3u8");
                }

                return NotFound();
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
