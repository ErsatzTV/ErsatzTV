using ErsatzTV.Core;

namespace ErsatzTV.Application.Watermarks;

public record DeleteWatermark(int WatermarkId) : IRequest<Either<BaseError, Unit>>;
