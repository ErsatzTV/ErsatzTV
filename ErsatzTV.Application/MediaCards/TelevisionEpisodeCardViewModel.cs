using System;

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
        string Poster) : MediaCardViewModel(
        EpisodeId,
        Title,
        $"Episode {Episode}",
        $"Episode {Episode}",
        Poster)
    {
    }
}
