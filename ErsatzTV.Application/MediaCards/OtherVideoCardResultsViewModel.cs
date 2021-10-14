using System.Collections.Generic;
using ErsatzTV.Core.Search;
using LanguageExt;

namespace ErsatzTV.Application.MediaCards
{
    public record OtherVideoCardResultsViewModel(
        int Count,
        List<OtherVideoCardViewModel> Cards,
        Option<SearchPageMap> PageMap);
}
