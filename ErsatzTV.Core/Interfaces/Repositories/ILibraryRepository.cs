using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface ILibraryRepository
{
    Task<LibraryPath> Add(LibraryPath libraryPath);
    Task<Option<Library>> GetLibrary(int libraryId);
    Task<Option<LocalLibrary>> GetLocal(int libraryId);
    Task<List<Library>> GetAll();
    Task<Unit> UpdateLastScan(Library library);
    Task<Unit> UpdateLastScan(LibraryPath libraryPath);
    Task<List<LibraryPath>> GetLocalPaths(int libraryId);
    Task<int> CountMediaItemsByPath(int libraryPathId);
    Task SetEtag(LibraryPath libraryPath, Option<LibraryFolder> knownFolder, string path, string etag);
    Task CleanEtagsForLibraryPath(LibraryPath libraryPath);
    Task<Option<int>> GetParentFolderId(LibraryPath libraryPath, string folder);
    Task<LibraryFolder> GetOrAddFolder(LibraryPath libraryPath, Option<int> maybeParentFolder, string folder);
    Task UpdateLibraryFolderId(MediaFile mediaFile, int libraryFolderId);
    Task UpdatePath(LibraryPath libraryPath, string normalizedLibraryPath);
}
