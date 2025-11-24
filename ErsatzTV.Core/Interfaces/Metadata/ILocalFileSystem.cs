namespace ErsatzTV.Core.Interfaces.Metadata;

public interface ILocalFileSystem
{
    Unit EnsureFolderExists(string folder);
    DateTime GetLastWriteTime(string path);
    IEnumerable<string> ListSubdirectories(string folder);
    IEnumerable<string> ListFiles(string folder);
    IEnumerable<string> ListFiles(string folder, string searchPattern);
    IEnumerable<string> ListFiles(string folder, params string[] searchPatterns);
    Task<Either<BaseError, Unit>> CopyFile(string source, string destination);
    Unit EmptyFolder(string folder);
    Task<byte[]> GetHash(string path);
    string GetCustomOrDefaultFile(string folder, string file);
}
