using System.Collections.Generic;

namespace ErsatzTV.Application.Search
{
    public record SearchResultAllItemsViewModel(
        List<int> MovieIds,
        List<int> ShowIds,
        List<int> ArtistIds,
        List<int> MusicVideoIds);
}
