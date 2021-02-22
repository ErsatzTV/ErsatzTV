using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record AddTelevisionEpisodeToSimpleMediaCollection
        (int MediaCollectionId, int TelevisionEpisodeId) : MediatR.IRequest<Either<BaseError, Unit>>;
}
