using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.Application.FFmpegProfiles;

public record UpdateFFmpegProfile(
    int FFmpegProfileId,
    string Name,
    int ThreadCount,
    bool Transcode,
    HardwareAccelerationKind HardwareAcceleration,
    VaapiDriver VaapiDriver,
    string VaapiDevice,
    int ResolutionId,
    bool NormalizeVideo,
    FFmpegProfileVideoFormat VideoFormat,
    int VideoBitrate,
    int VideoBufferSize,
    FFmpegProfileAudioFormat AudioFormat,
    int AudioBitrate,
    int AudioBufferSize,
    bool NormalizeLoudness,
    int AudioChannels,
    int AudioSampleRate,
    bool NormalizeAudio,
    bool NormalizeFramerate,
    bool DeinterlaceVideo) : IRequest<Either<BaseError, UpdateFFmpegProfileResult>>;