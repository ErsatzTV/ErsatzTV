using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMusicVideoRepository
    {
        Task<Either<BaseError, MediaItemScanResult<MusicVideo>>> GetOrAdd(
            Artist artist,
            LibraryPath libraryPath,
            string path);

        Task<IEnumerable<string>> FindMusicVideoPaths(LibraryPath libraryPath);
        Task<List<int>> DeleteByPath(LibraryPath libraryPath, string path);
        Task<bool> AddGenre(MusicVideoMetadata metadata, Genre genre);
        Task<bool> AddTag(MusicVideoMetadata metadata, Tag tag);
        Task<bool> AddStudio(MusicVideoMetadata metadata, Studio studio);
        Task<List<MusicVideoMetadata>> GetMusicVideosForCards(List<int> ids);
        Task<Option<MusicVideo>> GetMusicVideo(int musicVideoId);
        Task<IEnumerable<string>> FindOrphanPaths(LibraryPath libraryPath);
    }
}
