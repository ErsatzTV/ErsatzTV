using ErsatzTV.Core;

namespace ErsatzTV.Application.FFmpegProfiles;

public record DeleteFFmpegProfile(int FFmpegProfileId) : MediatR.IRequest<Either<BaseError, Unit>>;