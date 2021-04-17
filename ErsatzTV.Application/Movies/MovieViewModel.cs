﻿using System.Collections.Generic;
using System.Globalization;
using ErsatzTV.Application.MediaCards;

namespace ErsatzTV.Application.Movies
{
    public record MovieViewModel(
        string Title,
        string Year,
        string Plot,
        string Poster,
        string FanArt,
        List<string> Genres,
        List<string> Tags,
        List<string> Studios,
        List<CultureInfo> Languages,
        List<ActorCardViewModel> Actors);
}
