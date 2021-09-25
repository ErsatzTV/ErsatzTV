using System.Collections.Generic;

namespace ErsatzTV.Application.Search
{
    public record SearchResultAllItemsViewModel(
        List<int> MovieIds,
        List<int> ShowIds,
        List<int> SeasonIds,
        List<int> EpisodeIds,
        List<int> ArtistIds,
        List<int> MusicVideoIds);
}
