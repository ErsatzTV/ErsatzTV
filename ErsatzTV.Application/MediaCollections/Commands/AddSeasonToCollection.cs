using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record AddSeasonToCollection
        (int CollectionId, int SeasonId) : IRequest<Either<BaseError, CollectionUpdateResult>>;
}
