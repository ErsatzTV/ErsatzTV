using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IOtherVideoRepository
    {
        Task<Either<BaseError, MediaItemScanResult<OtherVideo>>> GetOrAdd(LibraryPath libraryPath, string path);
        Task<IEnumerable<string>> FindOtherVideoPaths(LibraryPath libraryPath);
        Task<List<int>> DeleteByPath(LibraryPath libraryPath, string path);
        Task<bool> AddTag(OtherVideoMetadata metadata, Tag tag);
        Task<List<OtherVideoMetadata>> GetOtherVideosForCards(List<int> ids);
        // Task<int> GetOtherVideoCount(int artistId);
        // Task<List<OtherVideoMetadata>> GetPagedOtherVideos(int artistId, int pageNumber, int pageSize);
    }
}
