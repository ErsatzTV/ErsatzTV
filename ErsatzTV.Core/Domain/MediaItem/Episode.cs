using System.Collections.Generic;
using System.Diagnostics;

namespace ErsatzTV.Core.Domain;

[DebuggerDisplay("{EpisodeMetadata[0].Title ?? \"[unknown episode]\"}")]
public class Episode : MediaItem
{
    public int SeasonId { get; set; }
    public Season Season { get; set; }
    public List<EpisodeMetadata> EpisodeMetadata { get; set; }
    public List<MediaVersion> MediaVersions { get; set; }
}