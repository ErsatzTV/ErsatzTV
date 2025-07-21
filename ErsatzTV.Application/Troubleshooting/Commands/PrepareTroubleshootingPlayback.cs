using CliWrap;
using ErsatzTV.Core;

namespace ErsatzTV.Application.Troubleshooting;

public record PrepareTroubleshootingPlayback(
    int MediaItemId,
    int FFmpegProfileId,
    int WatermarkId,
    bool StartFromBeginning)
    : IRequest<Either<BaseError, Command>>;
