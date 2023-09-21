using ErsatzTV.Application.Resolutions;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.Application.FFmpegProfiles;

public record FFmpegProfileViewModel(
    int Id,
    string Name,
    int ThreadCount,
    HardwareAccelerationKind HardwareAcceleration,
    VaapiDriver VaapiDriver,
    string VaapiDevice,
    int? QsvExtraHardwareFrames,
    ResolutionViewModel Resolution,
    ScalingBehavior ScalingBehavior,
    FFmpegProfileVideoFormat VideoFormat,
    FFmpegProfileBitDepth BitDepth,
    int VideoBitrate,
    int VideoBufferSize,
    FFmpegProfileAudioFormat AudioFormat,
    int AudioBitrate,
    int AudioBufferSize,
    NormalizeLoudnessMode NormalizeLoudnessMode,
    int AudioChannels,
    int AudioSampleRate,
    bool NormalizeFramerate,
    bool DeinterlaceVideo);
