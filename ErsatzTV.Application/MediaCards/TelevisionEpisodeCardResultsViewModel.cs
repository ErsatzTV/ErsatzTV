using System.Collections.Generic;

namespace ErsatzTV.Application.MediaCards
{
    public record TelevisionEpisodeCardResultsViewModel(int Count, List<TelevisionEpisodeCardViewModel> Cards);
}
