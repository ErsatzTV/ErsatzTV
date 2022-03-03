using System.Collections.Generic;
using ErsatzTV.Core.Search;
using LanguageExt;

namespace ErsatzTV.Application.MediaCards;

public record TelevisionEpisodeCardResultsViewModel(
    int Count,
    List<TelevisionEpisodeCardViewModel> Cards,
    Option<SearchPageMap> PageMap);