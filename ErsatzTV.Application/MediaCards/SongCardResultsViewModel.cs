using System.Collections.Generic;
using ErsatzTV.Core.Search;
using LanguageExt;

namespace ErsatzTV.Application.MediaCards;

public record SongCardResultsViewModel(
    int Count,
    List<SongCardViewModel> Cards,
    Option<SearchPageMap> PageMap);