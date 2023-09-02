using ErsatzTV.Core.Domain;

namespace ErsatzTV.Scanner.Core.Interfaces.Metadata;

public interface ILocalSubtitlesProvider : IDisposable
{
    Task<bool> UpdateSubtitles(MediaItem mediaItem, Option<string> localPath, bool saveFullPath);
}
