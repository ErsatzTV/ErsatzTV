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
        Task<bool> UpdateLocalStatistics(MediaVersion mediaVersion);
        Task<bool> UpdatePlexStatistics(MediaVersion mediaVersion);
        Task<Unit> UpdateArtworkPath(Artwork artwork);
        Task<Unit> AddArtwork(Domain.Metadata metadata, Artwork artwork);
        Task<Unit> RemoveArtwork(Domain.Metadata metadata, ArtworkKind artworkKind);
        Task<Unit> MarkAsUpdated(ShowMetadata metadata, DateTime dateUpdated);
        Task<Unit> MarkAsUpdated(SeasonMetadata metadata, DateTime dateUpdated);
        Task<Unit> MarkAsUpdated(MovieMetadata metadata, DateTime dateUpdated);
    }
}
