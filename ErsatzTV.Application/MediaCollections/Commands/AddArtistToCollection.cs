using ErsatzTV.Core;
using LanguageExt;

namespace ErsatzTV.Application.MediaCollections.Commands
{
    public record AddArtistToCollection
        (int CollectionId, int ArtistId) : MediatR.IRequest<Either<BaseError, Unit>>;
}
