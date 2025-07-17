using CliWrap;
using ErsatzTV.Core;

namespace ErsatzTV.Application.Troubleshooting;

public record PrepareTroubleshootingPlayback(int MediaItemId, int FFmpegProfileId, int WatermarkId)
    : IRequest<Either<BaseError, Command>>;
