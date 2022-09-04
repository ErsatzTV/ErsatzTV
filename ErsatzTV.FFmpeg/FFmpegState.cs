using ErsatzTV.FFmpeg.OutputFormat;

namespace ErsatzTV.FFmpeg;

public record FFmpegState(
    bool SaveReport,
    HardwareAccelerationMode DecoderHardwareAccelerationMode,
    HardwareAccelerationMode EncoderHardwareAccelerationMode,
    Option<string> VaapiDriver,
    Option<string> VaapiDevice,
    Option<TimeSpan> Start,
    Option<TimeSpan> Finish,
    bool DoNotMapMetadata,
    Option<string> MetadataServiceProvider,
    Option<string> MetadataServiceName,
    Option<string> MetadataAudioLanguage,
    OutputFormatKind OutputFormat,
    Option<string> HlsPlaylistPath,
    Option<string> HlsSegmentTemplate,
    long PtsOffset,
    Option<int> ThreadCount,
    Option<int> MaybeQsvExtraHardwareFrames)
{
    public int QsvExtraHardwareFrames => MaybeQsvExtraHardwareFrames.IfNone(64);
    
    public static FFmpegState Concat(bool saveReport, string channelName) =>
        new(
            saveReport,
            HardwareAccelerationMode.None,
            HardwareAccelerationMode.None,
            Option<string>.None,
            Option<string>.None,
            Option<TimeSpan>.None,
            Option<TimeSpan>.None,
            true, // do not map metadata
            "ErsatzTV",
            channelName,
            Option<string>.None,
            OutputFormatKind.MpegTs,
            Option<string>.None,
            Option<string>.None,
            0,
            Option<int>.None,
            Option<int>.None);
}
