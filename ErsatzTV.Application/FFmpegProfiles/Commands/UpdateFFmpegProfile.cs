using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.Application.FFmpegProfiles;

public record UpdateFFmpegProfile(
    int FFmpegProfileId,
    string Name,
    int ThreadCount,
    bool NormalizeAudio,
    bool NormalizeVideo,
    HardwareAccelerationKind HardwareAcceleration,
    string VaapiDisplay,
    VaapiDriver VaapiDriver,
    string VaapiDevice,
    int? QsvExtraHardwareFrames,
    int ResolutionId,
    ScalingBehavior ScalingBehavior,
    FilterMode PadMode,
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
    double? TargetLoudness,
    int AudioChannels,
    int AudioSampleRate,
    bool NormalizeFramerate,
    bool NormalizeColors,
    bool DeinterlaceVideo) : IRequest<Either<BaseError, UpdateFFmpegProfileResult>>;
