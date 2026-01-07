using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.Maintenance;

public class DeleteOrphanedArtworkHandler(IArtworkRepository artworkRepository)
    : IRequestHandler<DeleteOrphanedArtwork, Either<BaseError, Unit>>
{
    public Task<Either<BaseError, Unit>>
        Handle(DeleteOrphanedArtwork request, CancellationToken cancellationToken) =>
        artworkRepository.GetOrphanedArtworkIds()
            .Bind(artworkRepository.Delete)
            .Map(_ => Right<BaseError, Unit>(Unit.Default));
}
