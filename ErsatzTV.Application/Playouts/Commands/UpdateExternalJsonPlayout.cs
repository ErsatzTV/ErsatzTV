using ErsatzTV.Core;

namespace ErsatzTV.Application.Playouts;

public record UpdateExternalJsonPlayout(int PlayoutId, string ExternalJsonFile)
    : IRequest<Either<BaseError, PlayoutNameViewModel>>;
