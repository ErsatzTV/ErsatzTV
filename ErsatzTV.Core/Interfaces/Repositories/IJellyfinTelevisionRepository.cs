using System.Collections.Generic;
using System.Threading.Tasks;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Jellyfin;
using LanguageExt;

namespace ErsatzTV.Core.Interfaces.Repositories
{
    public interface IJellyfinTelevisionRepository
    {
        Task<List<JellyfinItemEtag>> GetExistingShows(JellyfinLibrary library);
        Task<List<JellyfinItemEtag>> GetExistingSeasons(JellyfinLibrary library, string showItemId);
        Task<bool> AddShow(JellyfinShow show);
        Task<Unit> Update(JellyfinShow show);
        Task<bool> AddSeason(JellyfinSeason season);
        Task<Unit> Update(JellyfinSeason season);
    }
}
