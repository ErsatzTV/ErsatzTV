using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Jellyfin;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories;

public interface IJellyfinTelevisionRepository
{
    Task<List<JellyfinItemEtag>> GetExistingShows(JellyfinLibrary library);
    Task<List<JellyfinItemEtag>> GetExistingSeasons(JellyfinLibrary library, string showItemId);
    Task<List<JellyfinItemEtag>> GetExistingEpisodes(JellyfinLibrary library, string seasonItemId);
    Task<bool> AddShow(JellyfinShow show);
    Task<Option<JellyfinShow>> Update(JellyfinShow show);
    Task<bool> AddSeason(JellyfinShow show, JellyfinSeason season);
    Task<Option<JellyfinSeason>> Update(JellyfinSeason season);
    Task<bool> AddEpisode(JellyfinSeason season, JellyfinEpisode episode);
    Task<Option<JellyfinEpisode>> Update(JellyfinEpisode episode);
    Task<List<int>> RemoveMissingShows(JellyfinLibrary library, List<string> showIds);
    Task<Unit> RemoveMissingSeasons(JellyfinLibrary library, List<string> seasonIds);
    Task<List<int>> RemoveMissingEpisodes(JellyfinLibrary library, List<string> episodeIds);
    Task<Unit> DeleteEmptySeasons(JellyfinLibrary library);
    Task<List<int>> DeleteEmptyShows(JellyfinLibrary library);
}