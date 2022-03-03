using ErsatzTV.Core;

namespace ErsatzTV.Application.Playouts;

public record DeletePlayout(int PlayoutId) : IRequest<Either<BaseError, Unit>>;