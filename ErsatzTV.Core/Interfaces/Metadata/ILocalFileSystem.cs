using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata;

public interface ILocalFileSystem
{
    Unit EnsureFolderExists(string folder);
    DateTime GetLastWriteTime(string path);
    bool IsLibraryPathAccessible(LibraryPath libraryPath);
    IEnumerable<string> ListSubdirectories(string folder);
    IEnumerable<string> ListFiles(string folder);
    bool FileExists(string path);
    bool FolderExists(string folder);
    Task<Either<BaseError, Unit>> CopyFile(string source, string destination);
    Unit EmptyFolder(string folder);
}
