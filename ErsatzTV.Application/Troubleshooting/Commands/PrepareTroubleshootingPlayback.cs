using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Application.Troubleshooting;

public record PrepareTroubleshootingPlayback(
    int MediaItemId,
    int FFmpegProfileId,
    List<int> WatermarkIds,
    List<int> GraphicsElementIds,
    int? SubtitleId,
    Option<int> SeekSeconds)
    : IRequest<Either<BaseError, PlayoutItemResult>>;
