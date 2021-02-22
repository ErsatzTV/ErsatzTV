using System.Collections.Generic;

namespace ErsatzTV.Application.MediaCards
{
    public record MovieCardResultsViewModel(int Count, List<MovieCardViewModel> Cards);
}
