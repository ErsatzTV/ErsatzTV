using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record DeleteSmartCollection(int SmartCollectionId) : MediatR.IRequest<Either<BaseError, Unit>>;