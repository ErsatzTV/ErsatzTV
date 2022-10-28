using ErsatzTV.Core.Scripting;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IStreamSelectorRepository
{
    Task<EpisodeAudioStreamSelectorData> GetEpisodeData(int episodeId);
}
