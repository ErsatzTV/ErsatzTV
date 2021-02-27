using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using LanguageExt;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Metadata
{
    public class LocalFileSystem : ILocalFileSystem
    {
        public DateTime GetLastWriteTime(string path) =>
            Try(File.GetLastWriteTimeUtc(path)).IfFail(() => DateTime.MinValue);

        public bool IsLibraryPathAccessible(LibraryPath libraryPath) =>
            Directory.Exists(libraryPath.Path);

        public IEnumerable<string> ListSubdirectories(string folder) =>
            Try(Directory.EnumerateDirectories(folder)).IfFail(new List<string>());

        public IEnumerable<string> ListFiles(string folder) =>
            Try(Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly)).IfFail(new List<string>());

        public bool FileExists(string path) => File.Exists(path);
        public Task<byte[]> ReadAllBytes(string path) => File.ReadAllBytesAsync(path);

        public Unit CopyFile(string source, string destination)
        {
            string? directory = Path.GetDirectoryName(destination);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(source, destination, true);

            return Unit.Default;
        }
    }
}
