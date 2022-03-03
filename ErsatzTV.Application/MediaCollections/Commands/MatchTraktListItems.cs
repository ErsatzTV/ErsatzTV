using ErsatzTV.Core;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.MediaCollections;

public record MatchTraktListItems(int TraktListId, bool Unlock = true) : IRequest<Either<BaseError, Unit>>,
    IBackgroundServiceRequest;