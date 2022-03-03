using System.Collections.Generic;

namespace ErsatzTV.Application.MediaCards;

public record SearchCardResultsViewModel(
    List<MovieCardViewModel> MovieCards,
    List<TelevisionShowCardViewModel> ShowCards);