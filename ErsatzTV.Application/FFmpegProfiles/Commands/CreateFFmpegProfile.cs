using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.FFmpegProfiles.Commands
{
    public record CreateFFmpegProfile(
        string Name,
        int ThreadCount,
        bool Transcode,
        HardwareAccelerationKind HardwareAcceleration,
        VaapiDriver VaapiDriver,
        string VaapiDevice,
        int ResolutionId,
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
        bool NormalizeAudio,
        bool NormalizeFramerate) : IRequest<Either<BaseError, CreateFFmpegProfileResult>>;
}
