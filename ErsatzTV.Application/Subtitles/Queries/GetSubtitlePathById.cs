using ErsatzTV.Core;

namespace ErsatzTV.Application.Subtitles.Queries;

public record GetSubtitlePathById(int Id) : IRequest<Either<BaseError, string>>;
