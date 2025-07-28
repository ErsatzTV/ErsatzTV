using ErsatzTV.Core;

namespace ErsatzTV.Application.Streaming;

public record GetSeekTextSubtitleProcess(string SubtitlePath, TimeSpan Seek)
    : IRequest<Either<BaseError, SeekTextSubtitleProcess>>;
