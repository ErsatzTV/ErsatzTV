using ErsatzTV.Core;

namespace ErsatzTV.Application.Subtitles;

public record ExtractEmbeddedSubtitles(Option<int> PlayoutId) : IRequest<Either<BaseError, Unit>>,
    ISubtitleWorkerRequest;
