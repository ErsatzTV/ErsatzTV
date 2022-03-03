﻿using System.Collections.Generic;
using ErsatzTV.Core.Search;
using LanguageExt;

namespace ErsatzTV.Application.MediaCards;

public record TelevisionSeasonCardResultsViewModel(
    int Count,
    List<TelevisionSeasonCardViewModel> Cards,
    Option<SearchPageMap> PageMap);