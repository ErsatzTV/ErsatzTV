using System.Collections.Generic;
using System.Diagnostics;
using ErsatzTV.Core.Domain;

namespace ErsatzTV.Core.Scheduling
{
    [DebuggerDisplay("{First}")]
    public record GroupedMediaItem(MediaItem First, List<MediaItem> Additional);
}
