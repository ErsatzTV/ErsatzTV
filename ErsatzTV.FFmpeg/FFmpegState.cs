using ErsatzTV.FFmpeg.OutputFormat;
using LanguageExt;

namespace ErsatzTV.FFmpeg;

public record FFmpegState(
    bool SaveReport,
    HardwareAccelerationMode HardwareAccelerationMode,
    Option<string> VaapiDriver,
    Option<string> VaapiDevice,
    bool DoNotMapMetadata,
    Option<string> MetadataServiceProvider,
    Option<string> MetadataServiceName,
    Option<string> MetadataAudioLanguage,
    OutputFormatKind OutputFormat,
    Option<string> HlsPlaylistPath,
    Option<string> HlsSegmentTemplate,
    long PtsOffset)
{
    public static FFmpegState Concat(bool saveReport, string channelName) =>
        new(
            saveReport,
            HardwareAccelerationMode.None,
            Option<string>.None,
            Option<string>.None,
            true, // do not map metadata
            "ErsatzTV",
            channelName,
            Option<string>.None,
            OutputFormatKind.MpegTs,
            Option<string>.None,
            Option<string>.None,
            0);
}
