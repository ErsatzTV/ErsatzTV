using System.Collections.Generic;

namespace ErsatzTV.Application.MediaCards
{
    public record SimpleMediaCollectionCardResultsViewModel(
        string Name,
        List<MovieCardViewModel> MovieCards,
        List<TelevisionShowCardViewModel> ShowCards,
        List<TelevisionSeasonCardViewModel> SeasonCards,
        List<TelevisionEpisodeCardViewModel> EpisodeCards);
}
