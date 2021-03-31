using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.FFmpegProfiles.Commands
{
    public record CreateFFmpegProfile(
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
        int AudioVolume,
        int AudioChannels,
        int AudioSampleRate,
        bool NormalizeAudio,
        string FrameRate) : IRequest<Either<BaseError, FFmpegProfileViewModel>>;
}
