using ErsatzTV.FFmpeg.Format;
using LanguageExt;

namespace ErsatzTV.FFmpeg;

public record FrameState(
    bool Realtime,
    Option<TimeSpan> Start,
    Option<TimeSpan> Finish,
    string VideoFormat,
    IPixelFormat PixelFormat,
    FrameSize ScaledSize,
    FrameSize PaddedSize,
    Option<int> VideoBitrate,
    Option<int> VideoBufferSize,
    Option<int> VideoTrackTimeScale,
    string AudioFormat,
    int AudioChannels,
    Option<int> AudioBitrate,
    Option<int> AudioBufferSize,
    Option<int> AudioSampleRate,
    Option<TimeSpan> AudioDuration,
    FrameDataLocation FrameDataLocation = FrameDataLocation.Unknown);
