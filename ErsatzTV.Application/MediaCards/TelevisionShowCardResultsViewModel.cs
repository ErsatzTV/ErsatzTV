using System.Collections.Generic;
using ErsatzTV.Core.Search;
using LanguageExt;

namespace ErsatzTV.Application.MediaCards;

public record TelevisionShowCardResultsViewModel(
    int Count,
    List<TelevisionShowCardViewModel> Cards,
    Option<SearchPageMap> PageMap);