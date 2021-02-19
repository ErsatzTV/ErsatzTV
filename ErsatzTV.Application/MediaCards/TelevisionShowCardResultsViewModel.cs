using System.Collections.Generic;

namespace ErsatzTV.Application.MediaCards
{
    public record TelevisionShowCardResultsViewModel(int Count, List<TelevisionShowCardViewModel> Cards);
}
