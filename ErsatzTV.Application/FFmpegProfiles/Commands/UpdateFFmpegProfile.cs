using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.Application.FFmpegProfiles;

public record UpdateFFmpegProfile(
    int FFmpegProfileId,
    string Name,
    int ThreadCount,
    HardwareAccelerationKind HardwareAcceleration,
    VaapiDriver VaapiDriver,
    string VaapiDevice,
    int ResolutionId,
    FFmpegProfileVideoFormat VideoFormat,
    int VideoBitrate,
    int VideoBufferSize,
    FFmpegProfileAudioFormat AudioFormat,
    int AudioBitrate,
    int AudioBufferSize,
    bool NormalizeLoudness,
    int AudioChannels,
    int AudioSampleRate,
    bool NormalizeFramerate,
    bool DeinterlaceVideo,
    FFmpegProfileSubtitleMode SubtitleMode) : IRequest<Either<BaseError, UpdateFFmpegProfileResult>>;