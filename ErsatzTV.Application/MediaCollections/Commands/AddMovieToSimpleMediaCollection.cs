using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record AddMovieToSimpleMediaCollection
        (int MediaCollectionId, int MovieId) : MediatR.IRequest<Either<BaseError, Unit>>;
}
