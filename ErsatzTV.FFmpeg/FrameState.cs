using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.OutputFormat;
using LanguageExt;

namespace ErsatzTV.FFmpeg;

public record FrameState(
    bool SaveReport,
    HardwareAccelerationMode HardwareAccelerationMode,
    Option<string> VaapiDriver,
    Option<string> VaapiDevice,
    bool Realtime,
    bool InfiniteLoop,
    Option<TimeSpan> Start,
    Option<TimeSpan> Finish,
    string VideoFormat,
    Option<IPixelFormat> PixelFormat,
    FrameSize ScaledSize,
    FrameSize PaddedSize,
    Option<int> FrameRate,
    Option<int> VideoBitrate,
    Option<int> VideoBufferSize,
    Option<int> VideoTrackTimeScale,
    bool Deinterlaced,
    Option<string> AudioFormat,
    Option<int> AudioChannels,
    Option<int> AudioBitrate,
    Option<int> AudioBufferSize,
    Option<int> AudioSampleRate,
    Option<TimeSpan> AudioDuration,
    bool NormalizeLoudness,
    bool DoNotMapMetadata,
    Option<string> MetadataServiceProvider,
    Option<string> MetadataServiceName,
    Option<string> MetadataAudioLanguage,
    OutputFormatKind OutputFormat,
    Option<string> HlsPlaylistPath,
    Option<string> HlsSegmentTemplate,
    long PtsOffset,
    FrameDataLocation FrameDataLocation = FrameDataLocation.Unknown)
{
    public static FrameState Concat(bool saveReport, string channelName, FrameSize resolution) =>
        new(
            saveReport,
            HardwareAccelerationMode.None,
            Option<string>.None,
            Option<string>.None,
            true, // realtime
            true, // infinite loop
            Option<TimeSpan>.None,
            Option<TimeSpan>.None,
            Format.VideoFormat.Undetermined,
            Option<IPixelFormat>.None,
            resolution,
            resolution,
            Option<int>.None,
            Option<int>.None,
            Option<int>.None,
            Option<int>.None,
            false,
            Option<string>.None,
            Option<int>.None,
            Option<int>.None,
            Option<int>.None,
            Option<int>.None,
            Option<TimeSpan>.None,
            false, // normalize loudness
            true, // do not map metadata
            "ErsatzTV",
            channelName,
            Option<string>.None,
            OutputFormatKind.MpegTs,
            Option<string>.None,
            Option<string>.None,
            0);
}
