using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IArtworkRepository
{
    Task<List<Artwork>> GetOrphanedArtwork();
    Task<Unit> Delete(List<Artwork> artwork);
}