using ErsatzTV.Core;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record AddMovieToCollection
        (int CollectionId, int MovieId) : IRequest<Either<BaseError, CollectionUpdateResult>>;
}
