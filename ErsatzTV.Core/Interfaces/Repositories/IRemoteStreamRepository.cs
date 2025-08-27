using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Metadata;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IRemoteStreamRepository
{
    Task<Either<BaseError, MediaItemScanResult<RemoteStream>>> GetOrAdd(
        LibraryPath libraryPath,
        LibraryFolder libraryFolder,
        string path,
        CancellationToken cancellationToken);

    Task<IEnumerable<string>> FindRemoteStreamPaths(LibraryPath libraryPath);
    Task<List<int>> DeleteByPath(LibraryPath libraryPath, string path);
    Task<bool> AddTag(RemoteStreamMetadata metadata, Tag tag);
    Task UpdateDefinition(RemoteStream remoteStream);
}
