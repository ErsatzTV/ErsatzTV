using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record MatchTraktListItems(int TraktListId, bool Unlock = true) : IRequest<Either<BaseError, Unit>>,
    IBackgroundServiceRequest;
