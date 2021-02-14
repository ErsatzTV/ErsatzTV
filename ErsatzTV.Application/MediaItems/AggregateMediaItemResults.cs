using System.Collections.Generic;

namespace ErsatzTV.Application.MediaItems
{
    public record AggregateMediaItemResults(int Count, List<AggregateMediaItemViewModel> DataPage);
}
