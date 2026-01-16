using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IPlexMetadataRepository
{
    Task<Unit> RemoveArtwork(Domain.Metadata metadata, ArtworkKind artworkKind);
}
