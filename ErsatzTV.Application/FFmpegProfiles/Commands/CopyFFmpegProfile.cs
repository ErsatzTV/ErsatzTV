using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.FFmpegProfiles.Commands
{
    public record CopyFFmpegProfile
        (int FFmpegProfileId, string Name) : IRequest<Either<BaseError, FFmpegProfileViewModel>>;
}
