using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.FFmpegProfiles;

public record CopyFFmpegProfile
    (int FFmpegProfileId, string Name) : IRequest<Either<BaseError, FFmpegProfileViewModel>>;