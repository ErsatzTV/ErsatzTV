using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.FFmpegProfiles;

public record DeleteFFmpegProfile(int FFmpegProfileId) : MediatR.IRequest<Either<BaseError, Unit>>;