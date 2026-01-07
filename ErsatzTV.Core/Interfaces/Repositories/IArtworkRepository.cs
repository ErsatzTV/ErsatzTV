namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IArtworkRepository
{
    Task<List<int>> GetOrphanedArtworkIds();
    Task<Unit> Delete(List<int> artworkIds);
}
