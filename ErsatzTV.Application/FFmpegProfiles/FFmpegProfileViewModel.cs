using ErsatzTV.Application.Resolutions;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Application.FFmpegProfiles
{
    public record FFmpegProfileViewModel(
        int Id,
        string Name,
        int ThreadCount,
        bool Transcode,
        HardwareAccelerationKind HardwareAcceleration,
        ResolutionViewModel Resolution,
        bool NormalizeVideo,
        string VideoCodec,
        int VideoBitrate,
        int VideoBufferSize,
        string AudioCodec,
        int AudioBitrate,
        int AudioBufferSize,
        int AudioVolume,
        int AudioChannels,
        int AudioSampleRate,
        bool NormalizeAudio,
        string FrameRate);
}
