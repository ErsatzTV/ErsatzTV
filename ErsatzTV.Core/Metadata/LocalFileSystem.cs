using System;
using System.Collections.Generic;
using System.IO;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Metadata;
using static LanguageExt.Prelude;

namespace ErsatzTV.Core.Metadata
{
    public class LocalFileSystem : ILocalFileSystem
    {
        public DateTime GetLastWriteTime(string path) =>
            Try(File.GetLastWriteTimeUtc(path)).IfFail(() => DateTime.MinValue);

        public bool IsMediaSourceAccessible(LocalMediaSource localMediaSource) =>
            Directory.Exists(localMediaSource.Folder);

        public IEnumerable<string> ListSubdirectories(string folder) =>
            Try(Directory.EnumerateDirectories(folder)).IfFail(new List<string>());

        public IEnumerable<string> ListFiles(string folder) =>
            Try(Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly)).IfFail(new List<string>());

        public bool FileExists(string path) => File.Exists(path);
    }
}
