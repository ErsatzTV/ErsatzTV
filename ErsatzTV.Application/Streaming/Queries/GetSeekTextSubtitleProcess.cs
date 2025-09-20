using ErsatzTV.Application.Subtitles;
using ErsatzTV.Core;

namespace ErsatzTV.Application.Streaming;

public record GetSeekTextSubtitleProcess(SubtitlePathAndCodec PathAndCodec, TimeSpan Seek)
    : IRequest<Either<BaseError, SeekTextSubtitleProcess>>;
