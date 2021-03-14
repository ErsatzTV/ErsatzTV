using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMetadataRepository
    {
        Task<Unit> RemoveGenre(Genre genre);
        Task<Unit> UpdateStatistics(MediaVersion mediaVersion);
        Task<Unit> UpdateArtworkPath(Artwork artwork);
        Task<Unit> AddArtwork(Domain.Metadata metadata, Artwork artwork);
        Task<Unit> RemoveArtwork(Domain.Metadata metadata, ArtworkKind artworkKind);
    }
}
