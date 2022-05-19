using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IOtherVideoRepository
{
    Task<Either<BaseError, MediaItemScanResult<OtherVideo>>> GetOrAdd(LibraryPath libraryPath, string path);
    Task<IEnumerable<string>> FindOtherVideoPaths(LibraryPath libraryPath);
    Task<List<int>> DeleteByPath(LibraryPath libraryPath, string path);
    Task<bool> AddGenre(OtherVideoMetadata metadata, Genre genre);
    Task<bool> AddTag(OtherVideoMetadata metadata, Tag tag);
    Task<bool> AddStudio(OtherVideoMetadata metadata, Studio studio);
    Task<bool> AddActor(OtherVideoMetadata metadata, Actor actor);
    Task<bool> AddDirector(OtherVideoMetadata metadata, Director director);
    Task<bool> AddWriter(OtherVideoMetadata metadata, Writer writer);

    Task<List<OtherVideoMetadata>> GetOtherVideosForCards(List<int> ids);
}
