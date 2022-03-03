using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.Watermarks;

public record DeleteWatermark(int WatermarkId) : MediatR.IRequest<Either<BaseError, Unit>>;