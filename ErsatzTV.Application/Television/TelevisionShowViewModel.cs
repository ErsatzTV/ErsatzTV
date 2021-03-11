using System.Collections.Generic;

namespace ErsatzTV.Application.Television
{
    public record TelevisionShowViewModel(
        int Id,
        string Title,
        string Year,
        string Plot,
        string Poster,
        string FanArt,
        List<string> Genres,
        List<string> Tags);
}
