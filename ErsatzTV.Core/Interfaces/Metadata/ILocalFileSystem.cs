using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ILocalFileSystem
    {
        public DateTime GetLastWriteTime(string path);
        public bool IsMediaSourceAccessible(LocalMediaSource localMediaSource);
        public IEnumerable<string> ListSubdirectories(string folder);
        public IEnumerable<string> ListFiles(string folder);
        public bool FileExists(string path);
    }
}
