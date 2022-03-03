using System.Xml.Serialization;

namespace ErsatzTV.Infrastructure.Plex.Models;

public class PlexDirectorResponse
{
    [XmlAttribute("tag")]
    public string Tag { get; set; }
}