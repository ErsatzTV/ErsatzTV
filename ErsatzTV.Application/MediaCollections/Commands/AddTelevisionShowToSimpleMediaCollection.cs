using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record AddTelevisionShowToSimpleMediaCollection
        (int MediaCollectionId, int TelevisionShowId) : MediatR.IRequest<Either<BaseError, Unit>>;
}
