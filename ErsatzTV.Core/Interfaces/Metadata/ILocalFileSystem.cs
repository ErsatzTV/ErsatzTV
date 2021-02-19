using System;
using System.Collections.Generic;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata
{
    public interface ILocalFileSystem
    {
        DateTime GetLastWriteTime(string path);
        bool IsMediaSourceAccessible(LocalMediaSource localMediaSource);
        IEnumerable<string> ListSubdirectories(string folder);
        IEnumerable<string> ListFiles(string folder);
        bool FileExists(string path);
    }
}
