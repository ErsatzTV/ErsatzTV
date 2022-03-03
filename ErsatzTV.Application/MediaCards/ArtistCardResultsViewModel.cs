using System.Collections.Generic;
using ErsatzTV.Core.Search;
using LanguageExt;

namespace ErsatzTV.Application.MediaCards;

public record ArtistCardResultsViewModel(
    int Count,
    List<ArtistCardViewModel> Cards,
    Option<SearchPageMap> PageMap);