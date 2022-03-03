using ErsatzTV.Core;

namespace ErsatzTV.Application.MediaCollections;

public record DeleteTraktList(int TraktListId) : IRequest<Either<BaseError, LanguageExt.Unit>>,
    IBackgroundServiceRequest;