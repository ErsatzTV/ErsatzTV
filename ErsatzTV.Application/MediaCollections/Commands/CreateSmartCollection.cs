using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record CreateSmartCollection
    (string Query, string Name) : IRequest<Either<BaseError, SmartCollectionViewModel>>;
