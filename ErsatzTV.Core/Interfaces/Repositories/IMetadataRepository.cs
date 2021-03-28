using System;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMetadataRepository
    {
        Task<bool> RemoveGenre(Genre genre);
        Task<bool> RemoveTag(Tag tag);
        Task<bool> RemoveStudio(Studio studio);
        Task<bool> Update(Domain.Metadata metadata);
        Task<bool> Add(Domain.Metadata metadata);
        Task<bool> UpdateLocalStatistics(int mediaVersionId, MediaVersion incoming);
        Task<bool> UpdatePlexStatistics(int mediaVersionId, MediaVersion incoming);
        Task<Unit> UpdateArtworkPath(Artwork artwork);
        Task<Unit> AddArtwork(Domain.Metadata metadata, Artwork artwork);
        Task<Unit> RemoveArtwork(Domain.Metadata metadata, ArtworkKind artworkKind);
        Task<Unit> MarkAsUpdated(ShowMetadata metadata, DateTime dateUpdated);
        Task<Unit> MarkAsUpdated(SeasonMetadata metadata, DateTime dateUpdated);
        Task<Unit> MarkAsUpdated(MovieMetadata metadata, DateTime dateUpdated);
    }
}
