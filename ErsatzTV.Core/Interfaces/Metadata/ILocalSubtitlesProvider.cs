using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Interfaces.Metadata;

public interface ILocalSubtitlesProvider
{
    Task<bool> UpdateSubtitles(MediaItem mediaItem);
    Task<bool> UpdateSubtitleStreams(MediaItem mediaItem);
}
