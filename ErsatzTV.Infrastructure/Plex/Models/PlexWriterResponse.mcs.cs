using System.Xml.Serialization;

namespace ErsatzTV.Infrastructure.Plex.Models;

public class PlexWriterResponse
{
    [XmlAttribute("tag")]
    public string Tag { get; set; }
}
