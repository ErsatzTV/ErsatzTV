using System.Threading.Channels;
using ErsatzTV.Application;
using ErsatzTV.Application.MediaItems;
using ErsatzTV.Application.Troubleshooting;
using ErsatzTV.Application.Troubleshooting.Queries;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.FFmpeg;
using ErsatzTV.Core.Interfaces.Metadata;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Troubleshooting;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace ErsatzTV.Controllers.Api;

[ApiController]
public class TroubleshootController(
    ChannelWriter<IFFmpegWorkerRequest> channelWriter,
    ILocalFileSystem localFileSystem,
    IConfigElementRepository configElementRepository,
    ITroubleshootingNotifier notifier,
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
        StreamingMode streamingMode,
        [FromQuery]
        List<int> watermark,
        [FromQuery]
        List<int> graphicsElement,
        [FromQuery]
        string streamSelector,
        [FromQuery]
        int? subtitleId,
        [FromQuery]
        int seekSeconds,
        CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid();
        using var logContext = LogContext.PushProperty(InMemoryLogService.CorrelationIdKey, sessionId);

        try
        {
            Option<int> ss = seekSeconds > 0 ? seekSeconds : Option<int>.None;

            Either<BaseError, PlayoutItemResult> result = await mediator.Send(
                new PrepareTroubleshootingPlayback(
                    sessionId,
                    streamingMode,
                    mediaItem,
                    ffmpegProfile,
                    streamSelector,
                    watermark,
                    graphicsElement,
                    subtitleId,
                    ss),
                cancellationToken);

            if (result.IsLeft)
            {
                return NotFound();
            }

            foreach (PlayoutItemResult playoutItemResult in result.RightToSeq())
            {
                Either<BaseError, MediaItemInfo> maybeMediaInfo =
                    await mediator.Send(new GetMediaItemInfo(mediaItem), cancellationToken);
                foreach (MediaItemInfo mediaInfo in maybeMediaInfo.RightToSeq())
                {
                    try
                    {
                        TroubleshootingInfo troubleshootingInfo = await mediator.Send(
                            new GetTroubleshootingInfo(),
                            cancellationToken);

                        // filter ffmpeg profiles
                        troubleshootingInfo.FFmpegProfiles.RemoveAll(p => p.Id != ffmpegProfile);

                        // filter watermarks
                        troubleshootingInfo.Watermarks.RemoveAll(p => !watermark.Contains(p.Id));

                        await channelWriter.WriteAsync(
                            new StartTroubleshootingPlayback(
                                sessionId,
                                streamSelector,
                                playoutItemResult,
                                mediaInfo,
                                troubleshootingInfo),
                            cancellationToken);

                        string playlistFile = Path.Combine(FileSystemLayout.TranscodeTroubleshootingFolder, "live.m3u8");
                        while (!localFileSystem.FileExists(playlistFile))
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
                            if (cancellationToken.IsCancellationRequested || notifier.IsFailed(sessionId))
                            {
                                break;
                            }
                        }

                        int initialSegmentCount = await configElementRepository
                            .GetValue<int>(ConfigElementKey.FFmpegInitialSegmentCount, cancellationToken)
                            .Map(maybeCount => maybeCount.Match(c => c, () => 1));

                        initialSegmentCount = Math.Max(initialSegmentCount, 2);

                        bool hasSegments = false;
                        while (!hasSegments)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);

                            string[] segmentFiles = streamingMode switch
                            {
                                // StreamingMode.HttpLiveStreamingSegmenter => Directory.GetFiles(
                                //     FileSystemLayout.TranscodeTroubleshootingFolder,
                                //     "*.m4s"),
                                _ => Directory.GetFiles(FileSystemLayout.TranscodeTroubleshootingFolder, "*.ts")
                            };

                            if (segmentFiles.Length >= initialSegmentCount)
                            {
                                hasSegments = true;
                            }
                        }

                        if (!notifier.IsFailed(sessionId))
                        {
                            return Redirect("~/iptv/session/.troubleshooting/live.m3u8");
                        }
                    }
                    finally
                    {
                        notifier.RemoveSession(sessionId);
                    }
                }
            }
        }
        catch (Exception)
        {
            // do nothing
        }

        return NotFound();
    }

    [HttpHead("api/troubleshoot/playback/archive")]
    [HttpGet("api/troubleshoot/playback/archive")]
    public async Task<IActionResult> TroubleshootPlaybackArchive(
        [FromQuery]
        int mediaItem,
        [FromQuery]
        int ffmpegProfile,
        [FromQuery]
        StreamingMode streamingMode,
        [FromQuery]
        List<int> watermark,
        [FromQuery]
        List<int> graphicsElement,
        [FromQuery]
        int seekSeconds,
        CancellationToken cancellationToken)
    {
        Option<int> ss = seekSeconds > 0 ? seekSeconds : Option<int>.None;

        Option<string> maybeArchivePath = await mediator.Send(
            new ArchiveTroubleshootingResults(
                mediaItem,
                ffmpegProfile,
                streamingMode,
                watermark,
                graphicsElement,
                ss),
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
