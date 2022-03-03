using System.Collections.Generic;
using ErsatzTV.Core.Search;
using LanguageExt;

namespace ErsatzTV.Application.MediaCards;

public record MovieCardResultsViewModel(int Count, List<MovieCardViewModel> Cards, Option<SearchPageMap> PageMap);