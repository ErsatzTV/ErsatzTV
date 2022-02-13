using ErsatzTV.FFmpeg.Format;
using ErsatzTV.FFmpeg.OutputFormat;
using LanguageExt;

namespace ErsatzTV.FFmpeg;

public record FrameState(
    HardwareAccelerationMode HardwareAccelerationMode,
    bool Realtime,
    Option<TimeSpan> Start,
    Option<TimeSpan> Finish,
    string VideoFormat,
    IPixelFormat PixelFormat,
    FrameSize ScaledSize,
    FrameSize PaddedSize,
    Option<int> FrameRate,
    Option<int> VideoBitrate,
    Option<int> VideoBufferSize,
    Option<int> VideoTrackTimeScale,
    bool Deinterlaced,
    string AudioFormat,
    int AudioChannels,
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
    FrameDataLocation FrameDataLocation = FrameDataLocation.Unknown);
