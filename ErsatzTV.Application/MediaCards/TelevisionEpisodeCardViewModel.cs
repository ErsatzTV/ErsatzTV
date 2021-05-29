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
        int Episode,
        string Title,
        string Plot,
        string Poster,
        List<string> Directors,
        List<string> Writers) : MediaCardViewModel(
        EpisodeId,
        Title,
        $"Episode {Episode}",
        $"Episode {Episode}",
        Poster);
}
