using System.Diagnostics.CodeAnalysis;

namespace ErsatzTV.Core.Domain;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public class JellyfinCollection
{
    public int Id { get; set; }
    public string ItemId { get; set; }
    public string Etag { get; set; }
    public string Name { get; set; }
}
