using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record DeleteTraktList(int TraktListId) : IRequest<Either<BaseError, Unit>>,
    IBackgroundServiceRequest;
