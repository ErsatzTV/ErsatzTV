using System.Collections.Generic;

namespace ErsatzTV.Application.Artists
{
    public record ArtistViewModel(
        string Name,
        string Disambiguation,
        string Biography,
        string Thumbnail,
        string FanArt,
        List<string> Genres,
        List<string> Styles,
        List<string> Moods);
}
