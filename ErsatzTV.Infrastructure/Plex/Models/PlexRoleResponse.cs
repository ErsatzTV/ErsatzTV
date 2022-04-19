using System.Xml.Serialization;

namespace ErsatzTV.Infrastructure.Plex.Models;

public class PlexRoleResponse
{
    [XmlAttribute("tag")]
    public string Tag { get; set; }

    [XmlAttribute("role")]
    public string Role { get; set; }

    [XmlAttribute("thumb")]
    public string Thumb { get; set; }
}
