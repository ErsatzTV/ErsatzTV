using ErsatzTV.Core.Domain;
using ErsatzTV.Core.FFmpeg;

namespace ErsatzTV.Core.Interfaces.FFmpeg;

public interface ICustomStreamSelector
{
    Task<StreamSelectorResult> SelectStreams(Channel channel, MediaItemAudioVersion audioVersion, List<Subtitle> allSubtitles);
}
