using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface ILibraryRepository
{
    Task<LibraryPath> Add(LibraryPath libraryPath);
    Task<Option<Library>> Get(int libraryId);
    Task<Option<LocalLibrary>> GetLocal(int libraryId);
    Task<List<Library>> GetAll();
    Task<Unit> UpdateLastScan(Library library);
    Task<Unit> UpdateLastScan(LibraryPath libraryPath);
    Task<List<LibraryPath>> GetLocalPaths(int libraryId);
    Task<Option<LibraryPath>> GetPath(int libraryPathId);
    Task<int> CountMediaItemsByPath(int libraryPathId);
    Task<List<int>> GetMediaIdsByLocalPath(int libraryPathId);
    Task DeleteLocalPath(int libraryPathId);
    Task<Unit> SetEtag(LibraryPath libraryPath, Option<LibraryFolder> knownFolder, string path, string etag);
    Task<Unit> CleanEtagsForLibraryPath(LibraryPath libraryPath);
}