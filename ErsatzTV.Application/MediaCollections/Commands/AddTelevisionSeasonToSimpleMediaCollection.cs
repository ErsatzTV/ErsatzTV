using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record AddTelevisionSeasonToSimpleMediaCollection
        (int MediaCollectionId, int TelevisionSeasonId) : MediatR.IRequest<Either<BaseError, Unit>>;
}
