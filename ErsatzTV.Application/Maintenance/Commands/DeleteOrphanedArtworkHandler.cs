using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Application.Maintenance;

public class DeleteOrphanedArtworkHandler : MediatR.IRequestHandler<DeleteOrphanedArtwork, Either<BaseError, Unit>>
{
    private readonly IArtworkRepository _artworkRepository;

    public DeleteOrphanedArtworkHandler(IArtworkRepository artworkRepository) =>
        _artworkRepository = artworkRepository;

    public Task<Either<BaseError, Unit>>
        Handle(DeleteOrphanedArtwork request, CancellationToken cancellationToken) =>
        _artworkRepository.GetOrphanedArtwork()
            .Bind(_artworkRepository.Delete)
            .Map(_ => Right<BaseError, Unit>(Unit.Default));
}