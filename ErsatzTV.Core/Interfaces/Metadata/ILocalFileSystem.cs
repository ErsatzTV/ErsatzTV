using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ILocalFileSystem
    {
        Unit EnsureFolderExists(string folder);
        DateTime GetLastWriteTime(string path);
        bool IsLibraryPathAccessible(LibraryPath libraryPath);
        IEnumerable<string> ListSubdirectories(string folder);
        IEnumerable<string> ListFiles(string folder);
        bool FileExists(string path);
        Task<byte[]> ReadAllBytes(string path);
        Unit CopyFile(string source, string destination);
    }
}
