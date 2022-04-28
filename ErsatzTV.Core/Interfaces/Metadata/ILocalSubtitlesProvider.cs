using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata;

public interface ILocalSubtitlesProvider
{
    Task<bool> UpdateSubtitles(MediaItem mediaItem, Option<string> localPath, bool saveFullPath);
}
