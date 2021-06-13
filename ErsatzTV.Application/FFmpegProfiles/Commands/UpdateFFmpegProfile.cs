using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.FFmpegProfiles.Commands
{
    public record UpdateFFmpegProfile(
        int FFmpegProfileId,
        string Name,
        int ThreadCount,
        bool Transcode,
        HardwareAccelerationKind HardwareAcceleration,
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
        bool NormalizeAudio) : IRequest<Either<BaseError, UpdateFFmpegProfileResult>>;
}
