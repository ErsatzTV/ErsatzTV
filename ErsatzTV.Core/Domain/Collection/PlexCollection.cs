using System.Diagnostics.CodeAnalysis;

namespace ErsatzTV.Core.Domain;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public class PlexCollection
{
    public int Id { get; set; }
    public string Key { get; set; }
    public string Etag { get; set; }
    public string Name { get; set; }
}
