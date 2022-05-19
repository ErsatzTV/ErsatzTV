using ErsatzTV.Core;

namespace ErsatzTV.Application.FFmpegProfiles;

public record DeleteFFmpegProfile(int FFmpegProfileId) : IRequest<Either<BaseError, Unit>>;
