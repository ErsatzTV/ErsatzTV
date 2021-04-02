using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IMusicVideoRepository
    {
        Task<Option<MusicVideo>> GetByMetadata(LibraryPath libraryPath, MusicVideoMetadata metadata);

        Task<Either<BaseError, MediaItemScanResult<MusicVideo>>> Add(
            LibraryPath libraryPath,
            string filePath,
            MusicVideoMetadata metadata);

        Task<IEnumerable<string>> FindMusicVideoPaths(LibraryPath libraryPath);
        Task<List<int>> DeleteByPath(LibraryPath libraryPath, string path);
        Task<bool> AddGenre(MusicVideoMetadata metadata, Genre genre);
        Task<bool> AddTag(MusicVideoMetadata metadata, Tag tag);
        Task<bool> AddStudio(MusicVideoMetadata metadata, Studio studio);
        Task<List<MusicVideoMetadata>> GetMusicVideosForCards(List<int> ids);
        Task<Option<MusicVideo>> GetMusicVideo(int musicVideoId);
    }
}
