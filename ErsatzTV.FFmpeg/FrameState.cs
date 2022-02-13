using ErsatzTV.FFmpeg.Format;
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
    Option<string> MetadataServiceProvider,
    Option<string> MetadataServiceName,
    Option<string> MetadataAudioLanguage,
    FrameDataLocation FrameDataLocation = FrameDataLocation.Unknown);
