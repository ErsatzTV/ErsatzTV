using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using CliWrap;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Interfaces.Streaming;
using ErsatzTV.Core.Interfaces.Troubleshooting;
using ErsatzTV.Core.Notifications;
using ErsatzTV.FFmpeg.Runtime;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Troubleshooting;

public class StartTroubleshootingPlaybackHandler(
    ITroubleshootingNotifier notifier,
    IMediator mediator,
    IEntityLocker entityLocker,
    IRuntimeInfo runtimeInfo,
    IGraphicsEngine graphicsEngine,
    ILogger<StartTroubleshootingPlaybackHandler> logger)
    : IRequestHandler<StartTroubleshootingPlayback>
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public async Task Handle(StartTroubleshootingPlayback request, CancellationToken cancellationToken)
    {
        try
        {
            // write media info without title
            string infoJson = JsonSerializer.Serialize(request.MediaItemInfo with { Title = null }, Options);
            await File.WriteAllTextAsync(
                Path.Combine(FileSystemLayout.TranscodeTroubleshootingFolder, "media_info.json"),
                infoJson,
                cancellationToken);

            // write troubleshooting info
            string troubleshootingInfoJson = JsonSerializer.Serialize(
                new
                {
                    request.TroubleshootingInfo.Version,
                    Environment = request.TroubleshootingInfo.Environment.OrderBy(x => x.Key)
                        .ToDictionary(x => x.Key, x => x.Value),
                    request.TroubleshootingInfo.Health,
                    request.TroubleshootingInfo.Cpus,
                    request.TroubleshootingInfo.VideoControllers,
                    request.TroubleshootingInfo.FFmpegSettings,
                    request.TroubleshootingInfo.FFmpegProfiles,
                    request.TroubleshootingInfo.Watermarks
                },
                Options);
            await File.WriteAllTextAsync(
                Path.Combine(FileSystemLayout.TranscodeTroubleshootingFolder, "troubleshooting_info.json"),
                troubleshootingInfoJson,
                cancellationToken);

            // write stream selector
            if (!string.IsNullOrWhiteSpace(request.StreamSelector))
            {
                string fullPath = Path.Combine(FileSystemLayout.ChannelStreamSelectorsFolder, request.StreamSelector);
                if (File.Exists(fullPath))
                {
                    File.Copy(
                        fullPath,
                        Path.Combine(FileSystemLayout.TranscodeTroubleshootingFolder, "stream-selector.yml"));
                }
            }

            HardwareAccelerationKind hwAccel = request.TroubleshootingInfo.FFmpegProfiles.Head().HardwareAcceleration;
            if (hwAccel is HardwareAccelerationKind.Qsv)
            {
                await File.WriteAllTextAsync(
                    Path.Combine(FileSystemLayout.TranscodeTroubleshootingFolder, "capabilities_qsv.txt"),
                    request.TroubleshootingInfo.QsvCapabilities,
                    cancellationToken);
            }

            if (hwAccel is HardwareAccelerationKind.Vaapi || hwAccel is HardwareAccelerationKind.Qsv &&
                runtimeInfo.IsOSPlatform(OSPlatform.Linux))
            {
                await File.WriteAllTextAsync(
                    Path.Combine(FileSystemLayout.TranscodeTroubleshootingFolder, "capabilities_vaapi.txt"),
                    request.TroubleshootingInfo.VaapiCapabilities,
                    cancellationToken);
            }

            if (hwAccel is HardwareAccelerationKind.Nvenc)
            {
                await File.WriteAllTextAsync(
                    Path.Combine(FileSystemLayout.TranscodeTroubleshootingFolder, "capabilities_nvidia.txt"),
                    request.TroubleshootingInfo.NvidiaCapabilities,
                    cancellationToken);
            }

            logger.LogDebug(
                "ffmpeg troubleshooting arguments {FFmpegArguments}",
                request.PlayoutItemResult.Process.Arguments);

            Option<Pipe> maybePipe = Option<Pipe>.None;

            try
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                Command processWithPipe = request.PlayoutItemResult.Process;
                foreach (GraphicsEngineContext graphicsEngineContext in request.PlayoutItemResult.GraphicsEngineContext)
                {
                    var pipe = new Pipe();
                    maybePipe = pipe;
                    processWithPipe =
                        processWithPipe.WithStandardInputPipe(PipeSource.FromStream(pipe.Reader.AsStream()));

                    // fire and forget graphics engine task
                    _ = graphicsEngine.Run(
                        graphicsEngineContext,
                        pipe.Writer,
                        linkedCts.Token);
                }

                CommandResult commandResult = await processWithPipe
                    .WithStandardErrorPipe(PipeTarget.Null)
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteAsync(linkedCts.Token);

                await mediator.Publish(
                    new PlaybackTroubleshootingCompletedNotification(commandResult.ExitCode),
                    linkedCts.Token);

                logger.LogDebug("Troubleshooting playback completed with exit code {ExitCode}", commandResult.ExitCode);

                if (commandResult.ExitCode != 0)
                {
                    await linkedCts.CancelAsync();
                    notifier.NotifyFailed(request.SessionId);
                }
            }
            catch (TaskCanceledException)
            {
                // do nothing
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                foreach (Pipe pipe in maybePipe)
                {
                    await pipe.Writer.CompleteAsync();
                }
            }
        }
        finally
        {
            entityLocker.UnlockTroubleshootingPlayback();
        }
    }
}
