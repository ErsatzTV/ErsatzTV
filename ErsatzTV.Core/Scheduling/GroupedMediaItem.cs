using System.Collections.Generic;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Scheduling
{
    public record GroupedMediaItem(MediaItem First, List<MediaItem> Additional);
}
