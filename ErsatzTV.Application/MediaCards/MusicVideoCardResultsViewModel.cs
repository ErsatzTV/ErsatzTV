using System.Collections.Generic;
using ErsatzTV.Core.Search;
using LanguageExt;

namespace ErsatzTV.Application.MediaCards;

public record MusicVideoCardResultsViewModel(
    int Count,
    List<MusicVideoCardViewModel> Cards,
    Option<SearchPageMap> PageMap);