using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using CliWrap;
using CliWrap.Buffered;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Locking;
using ErsatzTV.Core.Notifications;
using ErsatzTV.FFmpeg.Runtime;
using Microsoft.Extensions.Logging;

namespace ErsatzTV.Application.Troubleshooting;

public class StartTroubleshootingPlaybackHandler(
    IMediator mediator,
    IEntityLocker entityLocker,
    IRuntimeInfo runtimeInfo,
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

            HardwareAccelerationKind hwAccel = request.TroubleshootingInfo.FFmpegProfiles.Head().HardwareAcceleration;
            if (hwAccel is HardwareAccelerationKind.Qsv)
            {
                await File.WriteAllTextAsync(
                    Path.Combine(FileSystemLayout.TranscodeTroubleshootingFolder, "capabilities_qsv.txt"),
                    request.TroubleshootingInfo.QsvCapabilities,
                    cancellationToken);
            }

            if (hwAccel is HardwareAccelerationKind.Vaapi || (hwAccel is HardwareAccelerationKind.Qsv &&
                                                              runtimeInfo.IsOSPlatform(OSPlatform.Linux)))
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

            logger.LogDebug("ffmpeg troubleshooting arguments {FFmpegArguments}", request.Command.Arguments);

            BufferedCommandResult result = await request.Command
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync(cancellationToken);

            await mediator.Publish(
                new PlaybackTroubleshootingCompletedNotification(result.ExitCode),
                cancellationToken);

            logger.LogDebug("Troubleshooting playback completed with exit code {ExitCode}", result.ExitCode);
        }
        finally
        {
            entityLocker.UnlockTroubleshootingPlayback();
        }
    }
}
