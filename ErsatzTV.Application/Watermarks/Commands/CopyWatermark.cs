using ErsatzTV.Core;

namespace ErsatzTV.Application.Watermarks;

public record CopyWatermark
    (int WatermarkId, string Name) : IRequest<Either<BaseError, WatermarkViewModel>>;
