using System.Xml.Serialization;

namespace ErsatzTV.Infrastructure.Plex.Models;

public class PlexCollectionResponse
{
    [XmlAttribute("id")]
    public int Id { get; set; }

    [XmlAttribute("tag")]
    public string Tag { get; set; }
}
