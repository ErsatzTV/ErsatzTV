using System.Collections.Generic;

namespace ErsatzTV.Application.Movies
{
    public record MovieViewModel(
        string Title,
        string Year,
        string Plot,
        string Poster,
        string FanArt,
        List<string> Genres,
        List<string> Tags);
}
