using System.Diagnostics.CodeAnalysis;

namespace ErsatzTV.Core.Domain;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public class RemoteStream : MediaItem
{
    public List<RemoteStreamMetadata> RemoteStreamMetadata { get; set; }
    public List<MediaVersion> MediaVersions { get; set; }
}
