using System.Collections.Generic;
using System.Globalization;
using ErsatzTV.Application.MediaCards;

namespace ErsatzTV.Application.Movies
{
    public record MovieViewModel(
        string Title,
        string Year,
        string Plot,
        List<string> Genres,
        List<string> Tags,
        List<string> Studios,
        List<string> ContentRatings,
        List<CultureInfo> Languages,
        List<ActorCardViewModel> Actors,
        List<string> Directors,
        List<string> Writers)
    {
        public string Poster { get; set; }
        public string FanArt { get; set; }
    }
}
