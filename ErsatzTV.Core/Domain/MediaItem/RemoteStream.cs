using System.Diagnostics.CodeAnalysis;

namespace ErsatzTV.Core.Domain;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public class RemoteStream : MediaItem
{
    public string Url { get; set; }
    public string Script { get; set; }
    public TimeSpan? Duration { get; set; }
    public string FallbackQuery { get; set; }
    public bool IsLive { get; set; }
    public List<RemoteStreamMetadata> RemoteStreamMetadata { get; set; }
    public List<MediaVersion> MediaVersions { get; set; }
}
