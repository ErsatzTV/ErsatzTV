using ErsatzTV.Application.Resolutions;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.Application.FFmpegProfiles
{
    public record FFmpegProfileViewModel(
        int Id,
        string Name,
        int ThreadCount,
        bool Transcode,
        HardwareAccelerationKind HardwareAcceleration,
        VaapiDriver VaapiDriver,
        string VaapiDevice,
        ResolutionViewModel Resolution,
        bool NormalizeVideo,
        string VideoCodec,
        int VideoBitrate,
        int VideoBufferSize,
        string AudioCodec,
        int AudioBitrate,
        int AudioBufferSize,
        bool NormalizeLoudness,
        int AudioChannels,
        int AudioSampleRate,
        bool NormalizeAudio);
}
