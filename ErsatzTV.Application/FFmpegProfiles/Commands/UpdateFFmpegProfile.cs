using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.FFmpegProfiles.Commands
{
    public record UpdateFFmpegProfile(
        int FFmpegProfileId,
        string Name,
        int ThreadCount,
        bool Transcode,
        bool QsvAcceleration,
        int ResolutionId,
        bool NormalizeResolution,
        string VideoCodec,
        bool NormalizeVideoCodec,
        int VideoBitrate,
        int VideoBufferSize,
        string AudioCodec,
        bool NormalizeAudioCodec,
        int AudioBitrate,
        int AudioBufferSize,
        int AudioVolume,
        int AudioChannels,
        int AudioSampleRate,
        bool NormalizeAudio) : IRequest<Either<BaseError, FFmpegProfileViewModel>>;
}
