using System.Diagnostics;

namespace ErsatzTV.Core.Domain;

[DebuggerDisplay("{EpisodeMetadata[0].Title}")]
public class EmbyEpisode : Episode
{
    public string ItemId { get; set; }
    public string Etag { get; set; }
}
