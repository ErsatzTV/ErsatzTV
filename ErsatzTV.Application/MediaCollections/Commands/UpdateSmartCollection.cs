using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record UpdateSmartCollection(int Id, string Query) : IRequest<Either<BaseError, Unit>>;
