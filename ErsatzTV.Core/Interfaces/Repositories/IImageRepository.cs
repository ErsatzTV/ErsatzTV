using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IImageRepository
{
    Task<Either<BaseError, MediaItemScanResult<Image>>> GetOrAdd(
        LibraryPath libraryPath,
        LibraryFolder libraryFolder,
        string path,
        CancellationToken cancellationToken);

    Task<IEnumerable<string>> FindImagePaths(LibraryPath libraryPath);
    Task<List<int>> DeleteByPath(LibraryPath libraryPath, string path);
    Task<bool> AddTag(ImageMetadata metadata, Tag tag);
}
