using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record AddMovieToCollection(int CollectionId, int MovieId) : MediatR.IRequest<Either<BaseError, Unit>>;
}
