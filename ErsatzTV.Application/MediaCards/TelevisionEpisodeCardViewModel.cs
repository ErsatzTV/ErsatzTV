using System;
using System.Collections.Generic;

namespace ErsatzTV.Application.MediaCards
{
    public record TelevisionEpisodeCardViewModel
    (
        int EpisodeId,
        DateTime Aired,
        string ShowTitle,
        int ShowId,
        int SeasonId,
        int Season,
        int Episode,
        string Title,
        string SortTitle,
        string Plot,
        string Poster,
        List<string> Directors,
        List<string> Writers) : MediaCardViewModel(
        EpisodeId,
        Title,
        $"Episode {Episode}",
        SortTitle,
        Poster);
}
