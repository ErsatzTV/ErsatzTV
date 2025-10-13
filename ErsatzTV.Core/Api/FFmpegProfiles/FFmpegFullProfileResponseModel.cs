using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.Core.Api.FFmpegProfiles;

public record FFmpegFullProfileResponseModel(
    int Id,
    string Name,
    int ThreadCount,
    HardwareAccelerationKind HardwareAcceleration,
    string VaapiDisplay,
    VaapiDriver VaapiDriver,
    string VaapiDevice,
    int? QsvExtraHardwareFrames,
    string Resolution,
    ScalingBehavior ScalingBehavior,
    FFmpegProfileVideoFormat VideoFormat,
    string VideoProfile,
    string VideoPreset,
    bool AllowBFrames,
    FFmpegProfileBitDepth BitDepth,
    int VideoBitrate,
    int VideoBufferSize,
    FFmpegProfileTonemapAlgorithm TonemapAlgorithm,
    FFmpegProfileAudioFormat AudioFormat,
    int AudioBitrate,
    int AudioBufferSize,
    NormalizeLoudnessMode NormalizeLoudnessMode,
    int AudioChannels,
    int AudioSampleRate,
    bool NormalizeFramerate,
    bool? DeinterlaceVideo);
