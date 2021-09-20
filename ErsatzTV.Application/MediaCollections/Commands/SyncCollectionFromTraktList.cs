using ErsatzTV.Core;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record SyncCollectionFromTraktList
        (int CollectionId, string TraktListUrl) : IRequest<Either<BaseError, Unit>>;
}
