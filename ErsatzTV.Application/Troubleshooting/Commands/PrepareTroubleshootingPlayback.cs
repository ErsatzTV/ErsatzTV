using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Application.Troubleshooting;

public record PrepareTroubleshootingPlayback(
    int MediaItemId,
    int FFmpegProfileId,
    int WatermarkId,
    int? SubtitleId,
    bool StartFromBeginning)
    : IRequest<Either<BaseError, PlayoutItemResult>>;
