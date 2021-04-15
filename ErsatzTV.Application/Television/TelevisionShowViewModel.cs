using System.Collections.Generic;
using System.Globalization;

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
        List<string> Tags,
        List<string> Studios,
        List<CultureInfo> Languages,
        List<string> Actors);
}
