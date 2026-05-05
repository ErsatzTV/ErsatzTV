namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IArtworkRepository
{
    Task<int> DeleteOrphanedActors(int? max, CancellationToken cancellationToken);
    Task<int> DeleteOrphanedArtwork(int? max, CancellationToken cancellationToken);
}
