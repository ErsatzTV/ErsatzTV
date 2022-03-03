using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Emby;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IEmbyTelevisionRepository
{
    Task<List<EmbyItemEtag>> GetExistingShows(EmbyLibrary library);
    Task<List<EmbyItemEtag>> GetExistingSeasons(EmbyLibrary library, string showItemId);
    Task<List<EmbyItemEtag>> GetExistingEpisodes(EmbyLibrary library, string seasonItemId);
    Task<bool> AddShow(EmbyShow show);
    Task<Option<EmbyShow>> Update(EmbyShow show);
    Task<bool> AddSeason(EmbyShow show, EmbySeason season);
    Task<Option<EmbySeason>> Update(EmbySeason season);
    Task<bool> AddEpisode(EmbySeason season, EmbyEpisode episode);
    Task<Option<EmbyEpisode>> Update(EmbyEpisode episode);
    Task<List<int>> RemoveMissingShows(EmbyLibrary library, List<string> showIds);
    Task<Unit> RemoveMissingSeasons(EmbyLibrary library, List<string> seasonIds);
    Task<List<int>> RemoveMissingEpisodes(EmbyLibrary library, List<string> episodeIds);
    Task<Unit> DeleteEmptySeasons(EmbyLibrary library);
    Task<List<int>> DeleteEmptyShows(EmbyLibrary library);
}