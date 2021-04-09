using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IArtistRepository
    {
        Task<Option<Artist>> GetArtistByMetadata(int libraryPathId, ArtistMetadata metadata);

        Task<Either<BaseError, MediaItemScanResult<Artist>>> AddArtist(
            int libraryPathId,
            string artistFolder,
            ArtistMetadata metadata);

        Task<List<int>> DeleteEmptyArtists(LibraryPath libraryPath);
        Task<Option<Artist>> GetArtist(int artistId);
        Task<List<ArtistMetadata>> GetArtistsForCards(List<int> ids);
        Task<bool> AddGenre(ArtistMetadata metadata, Genre genre);
        Task<bool> AddStyle(ArtistMetadata metadata, Style style);
        Task<bool> AddMood(ArtistMetadata metadata, Mood mood);
    }
}
